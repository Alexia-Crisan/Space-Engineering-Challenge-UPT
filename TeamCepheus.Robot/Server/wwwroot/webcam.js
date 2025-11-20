const video = document.getElementById('video');

//#region WebRTC Stream

let defaultControls = false;
let reader = null;

const parseBoolString = (str, defaultVal) => {
    str = (str || '');

    if (['1', 'yes', 'true'].includes(str.toLowerCase())) {
        return true;
    }
    if (['0', 'no', 'false'].includes(str.toLowerCase())) {
        return false;
    }
    return defaultVal;
};

const loadAttributesFromQuery = () => {
    const params = new URLSearchParams(window.location.search);
    video.controls = parseBoolString(params.get('controls'), false);
    video.muted = parseBoolString(params.get('muted'), true);
    video.autoplay = parseBoolString(params.get('autoplay'), true);
    video.playsInline = parseBoolString(params.get('playsinline'), true);
    video.disablepictureinpicture = parseBoolString(params.get('disablepictureinpicture'), false);
    defaultControls = video.controls;
};

window.addEventListener('load', () => {
    loadAttributesFromQuery();

    reader = new MediaMTXWebRTCReader({
        url: (() => {
            const u = new URL('mystream/whep', window.location.href);
            u.port = '8889';
            u.search = window.location.search;
            return u.href;
        })(),
        onError: (err) => {
            console.error(err);
        },
        onTrack: (evt) => {
            video.srcObject = evt.streams[0];
        },
    });
});

window.addEventListener('beforeunload', () => {
    if (reader !== null) {
        reader.close();
    }
});

//#endregion

//#region QR Code scanner

const canvas = document.createElement("canvas");
const ctx = canvas.getContext("2d");

let isScanning = false;

function scanQr() {
    if (!isScanning) return;

    if (video.readyState === video.HAVE_ENOUGH_DATA) {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

        const imgData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const code = jsQR(imgData.data, imgData.width, imgData.height);

        if (code) {
            console.log("QR data:", code.data);

            if (doQrCheckpoint) {
                doQrCheckpoint(String(code.data));
            }
            
            isScanning = false;
            return; // stop scanning
        }
    }
    requestAnimationFrame(scanQr);
}

window.addEventListener('keydown', (ev) => {
    if (ev.key.toLowerCase() !== 'c') return;

    if (isScanning) {
        // stop scanning
        log('QR', "Stop scanning");
        isScanning = false;
        return;
    }

    // start scanning
    log('QR', "Start scanning");
    isScanning = true;
    requestAnimationFrame(scanQr);
});

//#endregion