(function () {
    var toggle = document.getElementById('themeToggle');
    var icon = document.getElementById('themeIcon');
    if (toggle && icon) {
        function updateIcon(isLight) {
            if (isLight) {
                icon.innerHTML = '<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>';
            } else {
                icon.innerHTML = '<circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>';
            }
        }
        var isCurrentlyLight = document.documentElement.getAttribute('data-theme') === 'light';
        updateIcon(isCurrentlyLight);

        toggle.addEventListener('click', function () {
            var isLight = document.documentElement.getAttribute('data-theme') === 'light';
            var next = isLight ? 'dark' : 'light';
            
            if (next === 'light') {
                document.documentElement.setAttribute('data-theme', 'light');
            } else {
                document.documentElement.removeAttribute('data-theme');
            }
            try { localStorage.setItem('theme', next); } catch (e) { }
            updateIcon(next === 'light');
            window.dispatchEvent(new Event('themeChanged'));
        });
    }

    // Show a small spinner on the button that submitted a form.
    document.addEventListener('submit', function (e) {
        var btn = e.submitter || e.target.querySelector('button[type=submit]');
        if (btn && btn.classList.contains('btn')) btn.classList.add('is-loading');
    });

    // Reset spinners when returning via the back button.
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('.is-loading').forEach(function (b) { b.classList.remove('is-loading'); });
    });

    // Copy-to-clipboard buttons: <button data-copy="text to copy">
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-copy]');
        if (!btn || !navigator.clipboard) return;
        var label = btn.textContent;
        navigator.clipboard.writeText(btn.getAttribute('data-copy')).then(function () {
            btn.textContent = 'Copied \u2713';
            setTimeout(function () { btn.textContent = label; }, 1600);
        });
    });
})();
