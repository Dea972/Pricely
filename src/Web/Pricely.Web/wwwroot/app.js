// Enter sends the message of a textarea marked data-submit-on-enter; Shift+Enter adds a line.
document.addEventListener('keydown', function (event) {
    var target = event.target;
    if (event.key !== 'Enter' || event.shiftKey || event.isComposing
        || !(target instanceof HTMLTextAreaElement) || !target.hasAttribute('data-submit-on-enter')) {
        return;
    }

    event.preventDefault();
    if (target.form) {
        target.form.requestSubmit();
    }
});
