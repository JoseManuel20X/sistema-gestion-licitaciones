# Despliegue en Kubernetes

Instrucciones reproducibles para desplegar la solución en un clúster
(enunciado §13.2).

## Requisitos

- Un clúster de Kubernetes. Sirve el que trae Docker Desktop
  (Ajustes → Kubernetes → *Enable Kubernetes*), `kind` o `minikube`.
- `kubectl` apuntando a ese clúster.

Comprobación:

```bash
kubectl get nodes
```

## Manifiestos

Los nueve archivos de `/k8s`, en el orden en que deben aplicarse:

| Archivo | Recurso | Función |
|---|---|---|
| `namespace.yaml` | Namespace | Aísla los recursos del proyecto |
| `app-configmap.yaml` | ConfigMap | Configuración no sensible |
| `app-secret.example.yaml` | Secret | **Plantilla**; se copia y se edita |
| `postgres-pvc.yaml` | PersistentVolumeClaim | Volumen de la base de datos |
| `postgres-service.yaml` | Service | Servicio headless de PostgreSQL |
| `postgres-statefulset.yaml` | StatefulSet | PostgreSQL 16 |
| `migraciones-job.yaml` | Job | Aplica migraciones y semilla una sola vez |
| `app-deployment.yaml` | Deployment | Dos réplicas de la interfaz web |
| `app-service.yaml` | Service | Expone la aplicación en el NodePort 30080 |

## Despliegue paso a paso

### 1. Construir la imagen

El Deployment usa `imagePullPolicy: IfNotPresent` y la etiqueta
`licitaciones-web:local`, de modo que toma la imagen construida en la máquina sin
necesidad de un registro:

```bash
docker build -f src/Licitaciones.Web/Dockerfile -t licitaciones-web:local .
```

Con `kind` hay que cargarla en el clúster:

```bash
kind load docker-image licitaciones-web:local
```

### 2. Crear el secreto

`app-secret.example.yaml` es una plantilla sin credenciales reales. Se copia, se
editan los valores y se aplica el resultado, que está en `.gitignore` para que
nunca se suba (§11):

```bash
cp k8s/app-secret.example.yaml k8s/app-secret.yaml
# editar POSTGRES_USER y POSTGRES_PASSWORD
```

### 3. Aplicar los manifiestos

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/app-configmap.yaml
kubectl apply -f k8s/app-secret.yaml
kubectl apply -f k8s/postgres-pvc.yaml
kubectl apply -f k8s/postgres-service.yaml
kubectl apply -f k8s/postgres-statefulset.yaml

# Esperar a que la base acepte conexiones antes de migrar
kubectl -n licitaciones rollout status statefulset/licitaciones-postgres

kubectl apply -f k8s/migraciones-job.yaml
kubectl -n licitaciones wait --for=condition=complete job/licitaciones-migraciones --timeout=180s

kubectl apply -f k8s/app-deployment.yaml
kubectl apply -f k8s/app-service.yaml
kubectl -n licitaciones rollout status deployment/licitaciones-web
```

La aplicación queda en `http://localhost:30080`.

## Decisiones de diseño

### Las migraciones van en un Job, no en el arranque

Es la decisión central de este despliegue. Con dos réplicas, si cada una aplicara
las migraciones al arrancar, competirían por aplicar la misma y una fallaría.

El Job las ejecuta **una sola vez**, antes de que las réplicas sirvan tráfico, y
deja constancia de su resultado en los logs. El Deployment recibe
`Migraciones__AplicarAlArrancar: "false"` desde el ConfigMap.

La misma imagen sirve para ambos usos: con el argumento `--solo-migrar` aplica
migraciones y semilla y termina sin levantar el servidor.

Fuera de Kubernetes, en Docker Compose, se sigue migrando al arrancar para que
`docker compose up --build` no requiera pasos manuales.

### StatefulSet para PostgreSQL, Deployment para la aplicación

La base tiene identidad y estado: su nombre de red debe ser estable y su volumen
no puede intercambiarse entre pods. La aplicación no guarda nada en memoria entre
peticiones, así que sus réplicas son intercambiables.

### El PVC se declara aparte

No se usa `volumeClaimTemplates` dentro del StatefulSet a propósito: así borrar y
recrear el StatefulSet no destruye los datos, que es justo lo que hay que
demostrar al reiniciar.

### Las tres probes y por qué son distintas

| Probe | Ruta | Qué decide |
|---|---|---|
| `startupProbe` | `/salud` | Da margen al arranque; hasta que no pasa, las otras no se evalúan, así un inicio lento no se confunde con un fallo |
| `readinessProbe` | `/salud` | Si el pod recibe tráfico. Comprueba también la base de datos: un pod que responde pero no la alcanza no sirve |
| `livenessProbe` | `/` | Si hay que reiniciar el pod. **No** consulta la base a propósito: si PostgreSQL cae, reiniciar la aplicación no arregla nada y solo provocaría un ciclo de reinicios |

### La cadena de conexión se arma en el pod

El ConfigMap aporta el host, el puerto y el nombre de la base; el Secret, el
usuario y la contraseña. La cadena completa se compone en la definición del
contenedor, de modo que la contraseña no aparezca en ningún manifiesto
versionado.

## Evidencia de despliegue

Recogida en un clúster local de Docker Desktop (kind, un nodo, Kubernetes
v1.36.1) el 2026-08-16.

### Pods

```
NAME                                READY   STATUS      RESTARTS   AGE
licitaciones-migraciones-pzt4r      0/1     Completed   0          8m19s
licitaciones-postgres-0             1/1     Running     0          8m46s
licitaciones-web-64d6467948-6d7zs   1/1     Running     0          30s
licitaciones-web-64d6467948-88h84   1/1     Running     0          24s
```

El Job aparece como `Completed`, no como `Running`: hizo su trabajo una vez y
terminó. Las dos réplicas de la aplicación están listas.

### Servicios

```
NAME                    TYPE        CLUSTER-IP      PORT(S)        AGE
licitaciones-postgres   ClusterIP   None            5432/TCP       14m
licitaciones-web        NodePort    10.96.132.222   80:30080/TCP   8m1s
```

PostgreSQL sin `CLUSTER-IP` porque es un servicio headless, como requiere el
StatefulSet.

### Volumen persistente

```
NAME                          STATUS   CAPACITY   ACCESS MODES   STORAGECLASS
licitaciones-postgres-datos   Bound    2Gi        RWO            standard
```

### Logs del Job de migraciones

```
INSERT INTO niveles_aprobacion ("Id", "Aprobador", "CreatedAt", "MontoMaximoCRC", "MontoMinimoCRC", "UpdatedAt")
VALUES (@p6, @p7, @p8, @p9, @p10, @p11);
INSERT INTO tipos_cambio ("Id", "Activo", "CRCporUSD", "CreatedAt", "FechaVigencia", "UpdatedAt")
VALUES (@p18, @p19, @p20, @p21, @p22, @p23);
```

La semilla del §11 se aplicó una sola vez, desde el Job.

### La aplicación responde

```
GET /        HTTP 200
GET /salud   HTTP 200   →   Healthy
```

### Conservación de datos tras reiniciar

La comprobación que pide el §13.2, de principio a fin:

```bash
# 1. Crear un dato a través de la interfaz
POST /Proveedores/Crear  →  HTTP 302   («Proveedor K8s 1840»)

# 2. Eliminar el pod de la base de datos
kubectl -n licitaciones delete pod licitaciones-postgres-0
#   pod "licitaciones-postgres-0" deleted

# 3. Kubernetes lo recrea solo
kubectl -n licitaciones rollout status statefulset/licitaciones-postgres
#   partitioned roll out complete: 1 new pods have been updated...
#   licitaciones-postgres-0   1/1   Running   0   6s

# 4. Consultar de nuevo
GET /Proveedores?filtro=K8s  →  «Proveedor K8s 1840»
```

**El registro sobrevivió**: el pod es nuevo, el dato es el mismo. Los datos viven
en el PersistentVolumeClaim, no en el contenedor.

### Dos fallos que solo aparecieron al desplegar

Ninguno lo habría detectado la validación de esquema, que daba 9 de 9 correctos.

**Las probes no expanden `$(VAR)`.** Kubernetes sustituye las variables de
entorno en `command` y `args` del contenedor, pero **no** dentro de los comandos
`exec` de las probes. `pg_isready` recibía el literal `$(POSTGRES_USER)` y el pod
entraba en ciclo de reinicios con «startup probe failed: no attempt». Se resolvió
pasando por un shell, que sí las expande.

**El proyecto Web no exponía `/salud`.** La comprobación de salud se había
añadido solo a la API. Las réplicas arrancaban, servían páginas y aun así
Kubernetes las mataba: la `startupProbe` recibía 404. Se añadió al Web la misma
comprobación, que verifica también el acceso a la base de datos.

### Nota sobre imágenes locales en kind

El clúster de Docker Desktop usa kind, cuyo nodo tiene su propio almacén de
imágenes. Una imagen construida en la máquina no está disponible dentro del
clúster hasta cargarla:

```bash
docker save licitaciones-web:local | \
  docker exec -i desktop-control-plane ctr --namespace k8s.io images import -
```

Con `kind` instalado por separado, el equivalente es `kind load docker-image`.

## Validación de los manifiestos

Se validan contra el esquema oficial sin necesidad de clúster, y la CI lo repite
en cada push:

```bash
docker run --rm -v "$(pwd)/k8s:/manifiestos" \
  ghcr.io/yannh/kubeconform:latest \
  -strict -summary -kubernetes-version 1.31.0 /manifiestos
```

La opción `-strict` rechaza campos desconocidos, que es como se detectan las
erratas en los nombres de propiedades.

La CI comprueba además que `k8s/app-secret.yaml` **no** esté versionado.

## Limpieza

```bash
kubectl delete namespace licitaciones
```

Borra todos los recursos del proyecto, incluido el PVC y sus datos.
