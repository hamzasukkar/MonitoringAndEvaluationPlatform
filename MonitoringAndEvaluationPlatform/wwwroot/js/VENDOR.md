# Vendored client-side libraries

These files are committed copies of third-party libraries. Nothing tracked their versions
or origins before, which meant no one could tell whether a bundled library carried a known
vulnerability. Record every change here.

## wwwroot/lib (managed copies)

| Library | Version | Source | Updated |
|---|---|---|---|
| jQuery | 3.7.1 | https://code.jquery.com/jquery-3.7.1.js | 2026-08-04 |
| Bootstrap (JS) | 5.3.3 | https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/ | 2026-08-04 |
| jQuery Validation | 1.21.0 | https://cdn.jsdelivr.net/npm/jquery-validation@1.21.0/dist/ | 2026-08-04 |
| jQuery Validation Unobtrusive | 4.0.0 | (unchanged; 4.0.0 is current) | — |
| Leaflet | 1.9.4 | (unchanged) | — |

Bootstrap JS was 5.1.0, which is affected by the tooltip/popover sanitizer XSS fixed in
5.2. The layouts were already loading Bootstrap **5.3** CSS from a CDN against that 5.1 JS,
so this upgrade also removes a CSS/JS version mismatch.

## wwwroot/js (unversioned vendored copies)

These were committed with no version marker and are still unpinned. Identify and record
each one, then move it to `wwwroot/lib` under the table above:

- `chart.js`, `chartjs-plugin-datalabels`
- `select2.min.js`
- `sweetalert2`
- `jstree.min.js`
- `orgchart.js`

## CDN references

Several layouts load Bootstrap, FontAwesome and other assets from CDNs without
Subresource Integrity attributes. Either add `integrity` + `crossorigin="anonymous"`, or
self-host them under `wwwroot/lib` and add them to the table above. Until then a CDN
compromise executes with full script privileges on this origin.

## Updating

1. Download the new version from the official source in the table.
2. Replace the files under `wwwroot/lib/<library>/`.
3. Update the version and date here.
4. Re-test the UI that depends on it - for Bootstrap that means modals, dropdowns,
   tooltips, tabs and collapse.
