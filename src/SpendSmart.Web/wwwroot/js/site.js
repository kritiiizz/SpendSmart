// Auto-submit filter forms when a select changes (progressive enhancement; the Filter button still works without JS).
document.querySelectorAll('form[data-auto-submit] select').forEach(function (select) {
  select.addEventListener('change', function () { select.form.submit(); });
});
