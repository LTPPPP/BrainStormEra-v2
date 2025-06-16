$(document).ready(function() {
    // Show validation summary if there are errors
    if ($('.validation-summary ul li').length > 0) {
        $('.alert-danger').show();
    }

    // Auto-focus on first input
    $('#username').focus();

    // Enter key handling
    $('#password').on('keypress', function(e) {
        if (e.which === 13) {
            $('#loginForm').submit();
        }
    });
}); 