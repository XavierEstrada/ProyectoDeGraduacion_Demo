(function ($) {
    "use strict";

    var fullHeight = function () {
        $('.js-fullheight').css('height', $(window).height());
        $(window).resize(function () {
            $('.js-fullheight').css('height', $(window).height());
        });
    };
    fullHeight();

    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
    });

    // Manejar el despliegue de submenús
    $('#sidebar ul li a.has-submenu').on('click', function (e) {
        e.preventDefault(); // Evitar redirección
        $(this).toggleClass('active'); // Alternar clase activa
        $(this).siblings('.submenu').slideToggle(); // Desplegar/plegar submenú
    });

})(jQuery);