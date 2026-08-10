// Alpine.js component registrations + htmx global behavior for the SMPP portal.
// Alpine.data() components are picked up automatically by Alpine's MutationObserver whenever
// htmx swaps new markup in, so none of this needs to be re-wired manually after navigation.

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

    document.addEventListener('alpine:init', function () {
        // Live "N characters - M part(s) - K credits each" readout under a message textarea.
        Alpine.data('messageCounter', function (config) {
            return {
                message: '',
                get text() {
                    var unicode = isUnicode(this.message);
                    var segments = countSegments(this.message, unicode);
                    var text = config.template
                        .replace('{chars}', this.message.length)
                        .replace('{parts}', segments)
                        .replace('{encoding}', unicode ? config.unicodeLabel : config.latinLabel);

                    if (config.rate > 0) {
                        var credits = Math.round(segments * config.rate * 10000) / 10000;
                        text += ' · ' + config.creditsTemplate.replace('{credits}', credits);
                    }

                    return text;
                },
            };
        });

        // Sender ID: the account either picks from its assigned list or types its own. Only greys
        // out the field that is not in play - which one counts is decided again on the server.
        Alpine.data('senderIdToggle', function (useFree) {
            return { useFree: !!useFree };
        });

        // Shared mobile sidebar state, referenced from both the topbar toggle button and the
        // sidebar/backdrop themselves.
        Alpine.data('sidebar', function () {
            return {
                open: false,
                toggle: function () { this.open = !this.open; },
                close: function () { this.open = false; },
            };
        });

        // Flash alerts: dismissible immediately, and auto-dismiss after a few seconds so the user
        // isn't left cleaning up banners by hand.
        Alpine.data('dismissible', function (delayMs) {
            return {
                show: true,
                init: function () {
                    var self = this;
                    setTimeout(function () { self.show = false; }, delayMs || 6000);
                },
            };
        });

        // Generic show/hide modal, used with the .app-modal / .app-modal-backdrop CSS (not
        // Bootstrap's .modal, whose stylesheet default of display:none fights with x-show).
        Alpine.data('modal', function (initiallyOpen) {
            return { open: !!initiallyOpen };
        });

        // Disables its form's submit button (and shows a spinner) for the duration of the
        // request, guarding against double-submits on actions like Approve/Credit.
        Alpine.data('submitGuard', function () {
            return { sending: false };
        });

        // Dashboard's three Chart.js charts. Registered here (rather than in a per-page <script>)
        // so it re-initializes correctly however the Dashboard is reached: Chart.js itself is
        // loaded once in <head>, and x-init runs fresh every time this component's element is
        // inserted into the DOM, including after an hx-boost swap into the Dashboard route.
        Alpine.data('dashboardCharts', function (config) {
            return {
                init: function () {
                    var gridColor = '#e6e8f0';
                    Chart.defaults.font.family = "system-ui, -apple-system, 'Segoe UI', sans-serif";
                    Chart.defaults.color = '#6b7280';

                    this.renderChart('trendChart', 'line', {
                        labels: config.trend.map(function (p) { return p.date; }),
                        datasets: [
                            {
                                label: config.totalLabel,
                                data: config.trend.map(function (p) { return p.total; }),
                                borderColor: '#2a78d6', backgroundColor: '#2a78d6',
                                tension: 0.25, borderWidth: 2, pointRadius: 3,
                            },
                            {
                                label: config.deliveredLabel,
                                data: config.trend.map(function (p) { return p.delivered; }),
                                borderColor: '#1baf7a', backgroundColor: '#1baf7a',
                                tension: 0.25, borderWidth: 2, pointRadius: 3,
                            },
                        ],
                    }, {
                        responsive: true,
                        interaction: { mode: 'index', intersect: false },
                        plugins: { legend: { position: 'bottom' } },
                        scales: {
                            x: { grid: { display: false } },
                            y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: gridColor } },
                        },
                    });

                    this.renderChart('sourceChart', 'bar', {
                        labels: config.sourceBreakdown.map(function (s) { return s.label; }),
                        datasets: [{
                            label: config.messagesLabel,
                            data: config.sourceBreakdown.map(function (s) { return s.count; }),
                            backgroundColor: config.sourceBreakdown.map(function (s) { return s.color; }),
                            borderRadius: 4,
                            maxBarThickness: 56,
                        }],
                    }, {
                        responsive: true,
                        plugins: { legend: { display: false } },
                        scales: {
                            x: { grid: { display: false } },
                            y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: gridColor } },
                        },
                    });

                    this.renderChart('statusChart', 'bar', {
                        labels: [config.messagesLabel],
                        datasets: config.statusBreakdown.map(function (s) {
                            return { label: s.label, data: [s.count], backgroundColor: s.color };
                        }),
                    }, {
                        indexAxis: 'y',
                        responsive: true,
                        plugins: { legend: { display: false } },
                        scales: {
                            x: { stacked: true, beginAtZero: true, grid: { color: gridColor } },
                            y: { stacked: true, grid: { display: false } },
                        },
                    });
                },
                renderChart: function (canvasId, type, data, options) {
                    var canvas = document.getElementById(canvasId);
                    if (!canvas) {
                        return;
                    }
                    // Guards against duplicate Chart instances if init() ever runs twice for the
                    // same canvas (e.g. a stray double-swap).
                    var existing = Chart.getChart(canvas);
                    if (existing) {
                        existing.destroy();
                    }
                    new Chart(canvas, { type: type, data: data, options: options });
                },
            };
        });
    });

    // Slim top-of-page loading indicator for every htmx-driven request (boosted nav, filters,
    // row actions alike).
    document.addEventListener('htmx:beforeRequest', function () {
        document.documentElement.classList.add('htmx-loading');
    });
    document.addEventListener('htmx:afterRequest', function () {
        document.documentElement.classList.remove('htmx-loading');
    });
})();
