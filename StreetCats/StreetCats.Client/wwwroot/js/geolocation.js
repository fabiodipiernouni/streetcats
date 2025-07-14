// Helper JavaScript per geolocalizzazione
window.getCurrentPosition = function () {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation non supportata'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            position => {
                resolve({
                    coords: {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    }
                });
            },
            error => {
                console.warn('Errore geolocalizzazione:', error.message);
                // Restituisci posizione default invece di errore
                resolve({
                    coords: {
                        latitude: 40.8518, // Centro Napoli
                        longitude: 14.2681,
                        accuracy: 1000
                    }
                });
            },
            {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 60000
            }
        );
    });
};