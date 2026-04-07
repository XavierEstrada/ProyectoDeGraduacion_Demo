(function ($) {
    "use strict";

    // Toggle sidebar collapse
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('sidebar-collapsed');
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
        if (window.location.pathname.toLowerCase().indexOf($(this).attr('href').toLowerCase()) !== -1) {
            $(this).closest('.sidebar-submenu').show();
            $(this).closest('.sidebar-submenu').siblings('.has-submenu').addClass('open');
        }
    });

})(jQuery);
