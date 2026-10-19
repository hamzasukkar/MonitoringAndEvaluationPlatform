// Renders the authenticator enrolment QR code.
//
// Kept in its own file rather than inline in the Razor page so the page stays clean under
// a future enforcing Content-Security-Policy - see SECURITY-DECISIONS.md. The otpauth URI
// is passed through a data- attribute, never interpolated into script.
(function () {
    "use strict";

    var data = document.getElementById("qrCodeData");
    var target = document.getElementById("qrCode");

    if (!data || !target || typeof QRCode === "undefined") {
        return;
    }

    var uri = data.getAttribute("data-url");
    if (!uri) {
        return;
    }

    new QRCode(target, {
        text: uri,
        width: 200,
        height: 200,
        // High correction: the code is often scanned off a screen at an angle.
        correctLevel: QRCode.CorrectLevel.H
    });

    // The library renders both a <canvas> and an <img>; the alt text it sets is the raw
    // otpauth URI, which contains the shared secret. Replace it so the secret is not read
    // aloud by a screen reader or exposed by an image-alt inspector.
    var img = target.querySelector("img");
    if (img) {
        img.setAttribute("alt", "QR code for authenticator app enrolment");
    }
    var canvas = target.querySelector("canvas");
    if (canvas) {
        canvas.setAttribute("aria-label", "QR code for authenticator app enrolment");
    }
})();
