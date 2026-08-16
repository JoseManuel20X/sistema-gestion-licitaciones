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
