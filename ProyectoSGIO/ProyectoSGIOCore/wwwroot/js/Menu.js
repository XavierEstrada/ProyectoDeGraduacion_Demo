(function ($) {
    "use strict";

    // Toggle sidebar collapse
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('sidebar-collapsed');
        $('body').toggleClass('sidebar-open', $('#sidebar').hasClass('active'));
    });

    // En móvil/tablet, el sidebar es un panel superpuesto: tocar el fondo oscuro lo cierra
    $('#sidebarBackdrop').on('click', function () {
        $('#sidebar').removeClass('active');
        $('#content').removeClass('sidebar-collapsed');
        $('body').removeClass('sidebar-open');
    });

    // Al elegir un enlace del menú en móvil, cerrar el panel automáticamente
    $('#sidebar .sidebar-nav a').not('.has-submenu').on('click', function () {
        if (window.innerWidth <= 991.98) {
            $('#sidebar').removeClass('active');
            $('#content').removeClass('sidebar-collapsed');
            $('body').removeClass('sidebar-open');
        }
    });

    // Submenu toggle
    $('#sidebar .has-submenu').on('click', function (e) {
        e.preventDefault();
        var $link = $(this);
        var $submenu = $link.siblings('.sidebar-submenu');

        // Close other open submenus
        $('.sidebar-submenu').not($submenu).slideUp(200);
        $('.has-submenu').not($link).removeClass('open');

        $link.toggleClass('open');
        $submenu.slideToggle(200);
    });

    // Auto-open submenu if a child link is active
    $('.sidebar-submenu a').each(function () {
        var href = $(this).attr('href');
        if (href && href.length > 1 && window.location.pathname.toLowerCase().indexOf(href.toLowerCase()) !== -1) {
            $(this).closest('.sidebar-submenu').show();
            $(this).closest('.sidebar-submenu').siblings('.has-submenu').addClass('open');
        }
    });

})(jQuery);
