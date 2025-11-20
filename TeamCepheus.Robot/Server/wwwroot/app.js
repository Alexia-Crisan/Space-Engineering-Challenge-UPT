//#region Parameters

let currentSpeed = 50;
let turnSpeed = 20;
let servoAngle = 120;

// variables for data from robot
let temperature = 14.3;
let humidity = 60.1;
let pressure = 51.2;

//#endregion

//#region DOM Elements

const logEl = document.getElementById('log');
const overlayTopLeft = document.getElementById('overlay-top-left');
const overlayTopRight = document.getElementById('overlay-top-right');

//#endregion

//#region Display Status

function updateView() {
    overlayTopRight.innerText = 
`Speed: ${currentSpeed} / 100
Servo Angle: ${servoAngle} °
================
Temp: ${temperature} °C
Humidity: ${humidity} %
Pressure: ${pressure} kPa
`;
}

//#endregion

//#region WebSocket

let ws;

function log(source, ...args){
    logEl.textContent += `[${source}] ` + args.join(' ') + '\n';
    logEl.scrollTop = logEl.scrollHeight;
}

function connect(func) {
    const proto = location.protocol === 'https:' ? 'wss' : 'ws';
    ws = new WebSocket(proto + '://' + location.host + '/ws');
    ws.onopen = () => {
        log('WS', 'Connected');

        if (typeof func === "function") func();
    };
    ws.onclose = () => { log('WS', 'Disconnected'); setTimeout(connect, 1000); };
    ws.onmessage = (ev) => {
        try {
            const obj = JSON.parse(ev.data);

            if (obj.type === 'status') {
                temperature = obj.data.temperature.toFixed(1);
                humidity = obj.data.humidity.toFixed(1);
                pressure = obj.data.pressure.toFixed(1);

                updateView();
            } else {
                console.log("WS message", obj);
            }
        } catch(e) { 
            log('WS', 'Error parsing message');
            console.log('WS error', e);
        }
    };
}

function send(obj){
    if (!ws || ws.readyState !== WebSocket.OPEN){ log('[ws] not connected'); return; }
    const s = JSON.stringify(obj);
    ws.send(s);
}

//#endregion

//#region Driving commands

function brake() {
    send({ cmd: 'brake' });
}

function drive(forward=true) {
    send({ cmd: 'drive', forward });
}

function rotateLeft() {
    send({ cmd: 'rotateLeft' });
}

function rotateRight() {
    send({ cmd: 'rotateRight' });
}

function speed(motorA, motorB) {
    motorA = motorA ?? currentSpeed;
    motorB = motorB ?? currentSpeed;
    send({ cmd: 'speed', motorA, motorB });
}

function setServoAngle(angle) {
    servoAngle = angle;
    send({ cmd: 'servoAngle', angle });
}

document.addEventListener('keydown', (ev) => {
    const key = ev.key;
    
    switch(key) {
        case 'i':
        case 'I':
            currentSpeed = Math.min(currentSpeed + 1, 100);
            speed();
            break;
        case 'k':
        case 'K':
            currentSpeed = Math.max(0, currentSpeed - 1);
            speed();
            break;
        
        case 'o':
            servoAngle = Math.min(servoAngle + 5, 120);
            setServoAngle(servoAngle);
            break;
        case 'l':
            servoAngle = Math.max(0, servoAngle - 5);
            setServoAngle(servoAngle);
            break;
        
        case 'O':
            setServoAngle(120);
            break;
        case 'L':
            setServoAngle(0);
            break;

        case 'q':
            speed(currentSpeed, currentSpeed / 2);
            break;
        case 'e':
            speed(currentSpeed / 2, currentSpeed);
            break;

        case 'w': 
            if (!ev.repeat) {
                speed();
                drive(false);
            }
            break;
        case 'a':
            if (!ev.repeat) {
                speed(turnSpeed, turnSpeed);
                rotateRight();
            }
            break;
        case 's': 
            if (!ev.repeat) {
                speed();
                drive(true);
            }
            break;
        case 'd':
            if (!ev.repeat) {
                speed(turnSpeed, turnSpeed);
                rotateLeft();
            }
            break;
        case ' ': 
            if (!ev.repeat) {
                brake(); 
            }
            break;
    }

    updateView();
});

document.addEventListener('keyup', (ev) => {
    const key = ev.key.toLowerCase();
    switch(key) {
        case 'q':
        case 'e':
            speed();
            break;

        case 'w':
        case 'a':
        case 's':
        case 'd':
            brake();
            break;
    }
});

//#endregion

//#region Submit QR checkpoint

/**
 * Submit QR checkpoint
 * @param {string} url 
 */
function doQrCheckpoint(url) {
    const finalUrl = url
        .replace("YOUR_TEAM", "Cepheus")
        .replace("FILL_HERE", encodeURIComponent(String(temperature)))
        .replace("FILL_THERE", encodeURIComponent(String(humidity)));
    
    console.log("URL after replacement:", finalUrl);

    fetch(finalUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(data => {
            log('QR', 'Checkpoint submitted successfully!');
        })
        .catch(error => {
            log('QR', 'Error submitting checkpoint!');
            console.error('QR submission error:', error);
        });
}

//#endregion

//#region !!! Init

connect(() => {
    speed(currentSpeed, currentSpeed);
    setServoAngle(servoAngle);

    updateView();
});

//#endregion