// RaceDay — small progressive enhancements. Everything works without JavaScript.
(function () {
    // Live preview for image inputs (event banner, profile picture).
    document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
        input.addEventListener('change', function () {
            var target = document.querySelector(input.dataset.preview);
            var file = input.files && input.files[0];
            if (!target || !file || !file.type.startsWith('image/')) return;
            var reader = new FileReader();
            reader.onload = function (e) {
                target.innerHTML = '';
                var img = document.createElement('img');
                img.src = e.target.result;
                img.alt = 'Preview of selected image';
                if (target.dataset.imgClass) img.className = target.dataset.imgClass;
                target.appendChild(img);
            };
            reader.readAsDataURL(file);
        });
    });

    // Confirm destructive actions: <form data-confirm="Are you sure?">
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.dataset.confirm)) e.preventDefault();
        });
    });

    // Result entry: finish time / position are only needed for "Finished".
    document.querySelectorAll('[data-result-row]').forEach(function (row) {
        var status = row.querySelector('select[name="Status"]');
        var fields = row.querySelectorAll('[data-finished-only]');
        function sync() {
            var finished = status.value === 'Finished';
            fields.forEach(function (f) { f.disabled = !finished; if (!finished) f.value = ''; });
        }
        if (status) { status.addEventListener('change', sync); sync(); }
    });

    // Prevent double submits on slow uploads.
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (e.defaultPrevented) return;
            if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;
            var btn = form.querySelector('[type="submit"][data-busy-text]');
            if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>' + btn.dataset.busyText; }
        });
    });
})();
