/*
 * Behaviour for the shared _UnitSelect partial: the "+" button next to a unit dropdown lets a
 * user define a unit that is not in the list yet, without losing whatever they have already
 * typed into the surrounding form.
 *
 * Events are delegated from document, so controls added to the page after load (the Measures
 * inline edit rows, the FrameworkGoals modals) work with no extra wiring.
 *
 * New units are created through Units/CreateAjax, which returns the existing unit rather than
 * an error when the name is already defined — so a user typing a duplicate simply gets it
 * selected.
 */
(function () {
    'use strict';

    function wrapperOf(el) {
        return el.closest('[data-unit-select]');
    }

    /* The full unit list, parsed once from the JSON the partial renders. Kept in a variable so
       units added during this page's life are visible to selects that have not been filled in
       yet. */
    var unitOptions = null;

    function allUnits() {
        if (unitOptions) { return unitOptions; }
        var payload = document.getElementById('unit-select-options');
        try {
            unitOptions = payload ? JSON.parse(payload.textContent) : [];
        } catch (e) {
            unitOptions = [];
        }
        return unitOptions;
    }

    /* A deferred select ships with only its current value, so a page with hundreds of rows does
       not repeat every unit in every row. The rest arrive the first time the list is opened. */
    function ensurePopulated(select) {
        if (!select || select.dataset.unitSelectDeferred !== 'true') { return; }
        if (select.dataset.unitSelectFilled === 'true') { return; }

        var current = select.value;
        allUnits().forEach(function (unit) {
            if (select.querySelector('option[value="' + unit.code + '"]')) { return; }
            var option = document.createElement('option');
            option.value = unit.code;
            option.textContent = unit.name;
            select.appendChild(option);
        });

        select.dataset.unitSelectFilled = 'true';
        select.value = current;
    }

    /* Opening the list with a keyboard or a mouse both go through one of these. */
    ['mousedown', 'focus', 'keydown'].forEach(function (type) {
        document.addEventListener(type, function (event) {
            var select = event.target.closest ? event.target.closest('[data-unit-select-input]') : null;
            if (select) { ensurePopulated(select); }
        }, true);
    });

    /* The antiforgery token of whichever form is on the page. The partial deliberately does not
       render its own: a second identical __RequestVerificationToken field inside the host form
       would be posted twice and break validation of that form. */
    function antiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function showError(wrapper, message) {
        var box = wrapper.querySelector('[data-unit-select-error]');
        if (!box) { return; }
        box.textContent = message;
        box.classList.remove('d-none');
    }

    function clearError(wrapper) {
        var box = wrapper.querySelector('[data-unit-select-error]');
        if (box) { box.classList.add('d-none'); }
    }

    function setPanelVisible(wrapper, visible) {
        var panel = wrapper.querySelector('[data-unit-select-new]');
        if (!panel) { return; }
        panel.classList.toggle('d-none', !visible);
        if (visible) {
            clearError(wrapper);
            var name = panel.querySelector('[data-unit-select-name]');
            if (name) { name.focus(); }
        }
    }

    /* Adds the unit to EVERY unit dropdown on the page, not just the one that created it, so a
       page with several (the Measures table) does not end up with stale lists. */
    function addOptionEverywhere(unit) {
        /* Record it first, so a deferred select that is filled later still sees it. */
        var known = allUnits();
        if (!known.some(function (u) { return u.code === unit.code; })) {
            known.push({ code: unit.code, name: unit.displayName });
        }

        document.querySelectorAll('[data-unit-select-input]').forEach(function (select) {
            /* An unfilled deferred select would show this unit alone until it is opened, so
               leave it be — ensurePopulated will pick it up from the list above. */
            if (select.dataset.unitSelectDeferred === 'true' &&
                select.dataset.unitSelectFilled !== 'true') { return; }
            if (select.querySelector('option[value="' + unit.code + '"]')) { return; }
            var option = document.createElement('option');
            option.value = unit.code;
            option.textContent = unit.displayName;
            select.appendChild(option);
        });
    }

    function saveNewUnit(wrapper) {
        var nameInput = wrapper.querySelector('[data-unit-select-name]');
        var select = wrapper.querySelector('[data-unit-select-input]');
        var saveButton = wrapper.querySelector('[data-unit-select-save]');
        if (!nameInput || !select) { return; }

        var name = (nameInput.value || '').trim();
        if (!name) {
            showError(wrapper, 'Please enter a unit name.');
            return;
        }

        var body = new FormData();
        body.append('name', name);
        body.append('__RequestVerificationToken', antiForgeryToken());

        if (saveButton) { saveButton.disabled = true; }

        fetch('/Units/CreateAjax', {
            method: 'POST',
            body: body,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                if (!result || !result.success) {
                    showError(wrapper, (result && result.message) || 'Could not create the unit.');
                    return;
                }

                addOptionEverywhere(result.unit);
                ensurePopulated(select);
                if (!select.querySelector('option[value="' + result.unit.code + '"]')) {
                    var option = document.createElement('option');
                    option.value = result.unit.code;
                    option.textContent = result.unit.displayName;
                    select.appendChild(option);
                }
                select.value = result.unit.code;
                /* Let any page script listening for a unit change (the FrameworkGoals value
                   suffix, for one) react as if the user had picked it by hand. */
                select.dispatchEvent(new Event('change', { bubbles: true }));

                nameInput.value = '';
                setPanelVisible(wrapper, false);
            })
            .catch(function () {
                showError(wrapper, 'Could not create the unit. Please try again.');
            })
            .finally(function () {
                if (saveButton) { saveButton.disabled = false; }
            });
    }

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-unit-select-toggle]');
        if (toggle) {
            var wrapper = wrapperOf(toggle);
            var panel = wrapper && wrapper.querySelector('[data-unit-select-new]');
            if (panel) { setPanelVisible(wrapper, panel.classList.contains('d-none')); }
            return;
        }

        var cancel = event.target.closest('[data-unit-select-cancel]');
        if (cancel) {
            setPanelVisible(wrapperOf(cancel), false);
            return;
        }

        var save = event.target.closest('[data-unit-select-save]');
        if (save) {
            saveNewUnit(wrapperOf(save));
        }
    });

    /* Enter inside the new-unit box saves the unit; without this it would submit the host form
       and lose the rest of the entry. */
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Enter') { return; }
        if (!event.target.matches('[data-unit-select-name]')) { return; }
        event.preventDefault();
        saveNewUnit(wrapperOf(event.target));
    });
})();
