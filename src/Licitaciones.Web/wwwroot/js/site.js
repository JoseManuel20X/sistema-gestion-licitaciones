// Comportamiento común de la interfaz.

(function () {
    'use strict';

    // --- Modo claro y oscuro (HU-12) ---
    //
    // El tema ya se aplicó en el <head> para evitar el parpadeo; aquí solo se
    // gestiona el interruptor y se persiste la preferencia entre visitas.

    var raiz = document.documentElement;
    var boton = document.getElementById('botonTema');
    var icono = document.getElementById('iconoTema');
    var texto = document.getElementById('textoTema');

    function pintarBoton(tema) {
        if (!icono || !texto) {
            return;
        }

        var esOscuro = tema === 'dark';
        icono.textContent = esOscuro ? '☀' : '☾';
        texto.textContent = esOscuro ? ' Claro' : ' Oscuro';
        if (boton) {
            boton.setAttribute('aria-pressed', String(esOscuro));
        }
    }

    function aplicarTema(tema) {
        raiz.setAttribute('data-bs-theme', tema);
        localStorage.setItem('tema', tema);
        pintarBoton(tema);
    }

    pintarBoton(raiz.getAttribute('data-bs-theme') || 'light');

    if (boton) {
        boton.addEventListener('click', function () {
            var actual = raiz.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
            aplicarTema(actual === 'dark' ? 'light' : 'dark');
        });
    }

    // Si la persona no eligió tema, se sigue la preferencia del sistema aunque
    // cambie mientras la página está abierta.
    if (!localStorage.getItem('tema') && window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (evento) {
            raiz.setAttribute('data-bs-theme', evento.matches ? 'dark' : 'light');
            pintarBoton(evento.matches ? 'dark' : 'light');
        });
    }

    // --- Confirmación antes de eliminar (enunciado §8.9) ---
    //
    // Se enlaza por delegación para que también funcione en el contenido que se
    // añada a la página después de cargarla.
    document.addEventListener('submit', function (evento) {
        var formulario = evento.target;
        if (formulario instanceof HTMLFormElement && formulario.hasAttribute('data-confirmar')) {
            if (!window.confirm(formulario.getAttribute('data-confirmar'))) {
                evento.preventDefault();
            }
        }
    });
})();

// --- Alternar los montos entre colones y dólares (HU-10) ---
//
// La conversión es solo de presentación. Los valores oficiales están en colones
// y no se modifican nunca: cada monto conserva su importe original en el
// atributo data-monto-crc y se recalcula al vuelo (§8.8).

(function () {
    'use strict';

    var boton = document.getElementById('botonMoneda');
    var aviso = document.getElementById('avisoMoneda');

    if (!boton) {
        return;
    }

    var tipoCambio = null;
    var formateadorCRC = new Intl.NumberFormat('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var formateadorUSD = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function montos() {
        return document.querySelectorAll('[data-monto-crc]');
    }

    function pintar(moneda) {
        montos().forEach(function (elemento) {
            var crc = parseFloat(elemento.getAttribute('data-monto-crc'));
            if (isNaN(crc)) {
                return;
            }

            if (moneda === 'USD' && tipoCambio) {
                elemento.textContent = '$ ' + formateadorUSD.format(crc / tipoCambio.crcPorUsd);
            } else {
                elemento.textContent = '₡ ' + formateadorCRC.format(crc);
            }
        });

        boton.textContent = moneda;
        boton.setAttribute('aria-pressed', String(moneda === 'USD'));

        if (moneda === 'USD' && tipoCambio) {
            aviso.textContent = 'Montos convertidos a dólares con el tipo de cambio de '
                + formateadorCRC.format(tipoCambio.crcPorUsd) + ' colones por dólar, vigente desde el '
                + tipoCambio.fechaVigencia + '. Los valores oficiales siguen en colones.';
            aviso.classList.remove('d-none');
        } else {
            aviso.classList.add('d-none');
        }

        localStorage.setItem('moneda', moneda);
    }

    function alternar() {
        var actual = boton.textContent.trim();
        var destino = actual === 'CRC' ? 'USD' : 'CRC';

        if (destino === 'CRC') {
            pintar('CRC');
            return;
        }

        // El tipo de cambio se pide una sola vez y se reutiliza mientras dure la
        // página: no cambia entre dos clics.
        if (tipoCambio) {
            pintar('USD');
            return;
        }

        fetch('/TiposCambio/ConversionActiva')
            .then(function (respuesta) {
                if (!respuesta.ok) {
                    return respuesta.json().then(function (cuerpo) { throw new Error(cuerpo.mensaje); });
                }
                return respuesta.json();
            })
            .then(function (datos) {
                tipoCambio = datos;
                pintar('USD');
            })
            .catch(function (error) {
                // Sin tipo de cambio activo no hay conversión posible; se explica
                // en vez de dejar el botón sin efecto aparente.
                aviso.textContent = error.message || 'No hay un tipo de cambio activo para convertir a dólares.';
                aviso.classList.remove('d-none');
            });
    }

    boton.addEventListener('click', alternar);

    // Se restaura la preferencia, pero solo si la página tiene montos que mostrar.
    if (localStorage.getItem('moneda') === 'USD' && montos().length > 0) {
        alternar();
    }
})();
