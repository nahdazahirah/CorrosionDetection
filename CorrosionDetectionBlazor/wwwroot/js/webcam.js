window.webcamInterop = {
    stream: null,

    start: async function (videoElementId) {
        const video = document.getElementById(videoElementId);
        this.stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        video.srcObject = this.stream;

        // Tunggu video metadata siap supaya dapat dimensi asli
        await new Promise(resolve => {
            video.onloadedmetadata = () => resolve();
        });

        return [video.videoWidth, video.videoHeight];
    },

    stop: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }
        if (video) video.srcObject = null;
    },

    captureFrame: async function (videoElementId, canvasElementId) {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        const ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

        const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
        const arrayBuffer = await blob.arrayBuffer();
        return new Uint8Array(arrayBuffer);
    }
};