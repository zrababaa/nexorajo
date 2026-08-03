// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    'use strict';

    // Mirror of SMPP.Infrastructure.Segmenting.SegmentCounter. Keep the two in step: this is what
    // tells the user what a message will cost, and the server is what actually charges for it.
    var LATIN_SINGLE = 160;
    var LATIN_FIRST_OF_MULTI = 153;
    var UNICODE_SINGLE = 70;
    var UNICODE_FIRST_OF_MULTI = 67;

    function isUnicode(message) {
        for (var i = 0; i < message.length; i++) {
            var c = message.charCodeAt(i);
            if (c === 10 || c === 13 || c === 9) {
                continue; // newlines and tabs exist in GSM-7 too
            }
            if (c < 32 || c > 126) {
                return true;
            }
        }
        return false;
    }

    function countSegments(message, unicode) {
        if (message.length === 0) {
            return 0;
        }
        var single = unicode ? UNICODE_SINGLE : LATIN_SINGLE;
        if (message.length <= single) {
            return 1;
        }
        return Math.ceil(message.length / (unicode ? UNICODE_FIRST_OF_MULTI : LATIN_FIRST_OF_MULTI));
    }

    // Live "N characters - M part(s) - K credits each" readout under a message textarea.
    // Wired up by data attributes so the markup carries its own (localized) label templates.
    function initMessageCounters() {
        var counters = document.querySelectorAll('[data-message-counter-for]');

        Array.prototype.forEach.call(counters, function (counter) {
            var textarea = document.getElementById(counter.getAttribute('data-message-counter-for'));
            if (!textarea) {
                return;
            }

            var rate = parseFloat(counter.getAttribute('data-rate') || '0');
            var template = counter.getAttribute('data-template') || '';
            var creditsTemplate = counter.getAttribute('data-credits-template') || '';
            var unicodeLabel = counter.getAttribute('data-unicode-label') || 'Unicode';
            var latinLabel = counter.getAttribute('data-latin-label') || 'GSM';

            function render() {
                var message = textarea.value;
                var unicode = isUnicode(message);
                var segments = countSegments(message, unicode);

                var text = template
                    .replace('{chars}', message.length)
                    .replace('{parts}', segments)
                    .replace('{encoding}', unicode ? unicodeLabel : latinLabel);

                if (rate > 0) {
                    var credits = Math.round(segments * rate * 10000) / 10000;
                    text += ' · ' + creditsTemplate.replace('{credits}', credits);
                }

                counter.textContent = text;
            }

            textarea.addEventListener('input', render);
            render();
        });
    }

    // Sender ID: the account either picks from its assigned list or types its own. Only greys out
    // the field that is not in play - which one counts is decided again on the server.
    function initSenderIdToggles() {
        var toggles = document.querySelectorAll('[data-sender-toggle]');

        Array.prototype.forEach.call(toggles, function (toggle) {
            var scope = toggle.closest('form') || document;
            var select = scope.querySelector('[data-sender-select]');
            var free = scope.querySelector('[data-sender-free]');

            function render() {
                if (select) {
                    select.disabled = toggle.checked;
                }
                if (free) {
                    free.disabled = !toggle.checked;
                }
            }

            toggle.addEventListener('change', render);
            render();
        });
    }

    function initBlockingModals() {
        var modals = document.querySelectorAll('[data-show-on-load]');

        Array.prototype.forEach.call(modals, function (element) {
            new bootstrap.Modal(element).show();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initMessageCounters();
        initSenderIdToggles();
        initBlockingModals();
    });
})();
