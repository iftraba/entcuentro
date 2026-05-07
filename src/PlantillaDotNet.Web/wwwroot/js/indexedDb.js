window.PlantillaDotNetIndexedDb = (() => {
    const DB_NAME = 'myapp_db';
    const DB_VERSION = 1;
    let _db = null;

    function openDb(storeNames) {
        return new Promise((resolve, reject) => {
            if (_db) { resolve(_db); return; }
            const req = indexedDB.open(DB_NAME, DB_VERSION);
            req.onupgradeneeded = e => {
                const db = e.target.result;
                storeNames.forEach(name => {
                    if (!db.objectStoreNames.contains(name))
                        db.createObjectStore(name, { keyPath: 'id' });
                });
            };
            req.onsuccess = e => { _db = e.target.result; resolve(_db); };
            req.onerror = e => reject(e.target.error);
        });
    }

    function tx(storeName, mode, fn) {
        return openDb([storeName]).then(db => new Promise((resolve, reject) => {
            const t = db.transaction(storeName, mode);
            const store = t.objectStore(storeName);
            const req = fn(store);
            if (req) {
                req.onsuccess = e => resolve(e.target.result);
                req.onerror = e => reject(e.target.error);
            } else {
                t.oncomplete = () => resolve();
                t.onerror = e => reject(e.target.error);
            }
        }));
    }

    return {
        getAll: (storeName) =>
            tx(storeName, 'readonly', s => s.getAll()),

        getById: (storeName, id) =>
            tx(storeName, 'readonly', s => s.get(id)),

        put: (storeName, entity) =>
            tx(storeName, 'readwrite', s => s.put(entity)),

        delete: (storeName, id) =>
            tx(storeName, 'readwrite', s => s.delete(id)),

        getUnsynced: (storeName) =>
            openDb([storeName]).then(db => new Promise((resolve, reject) => {
                const t = db.transaction(storeName, 'readonly');
                const store = t.objectStore(storeName);
                const result = [];
                const req = store.openCursor();
                req.onsuccess = e => {
                    const cursor = e.target.result;
                    if (cursor) {
                        if (!cursor.value.isSynced) result.push(cursor.value);
                        cursor.continue();
                    } else {
                        resolve(result);
                    }
                };
                req.onerror = e => reject(e.target.error);
            })),

        putBatch: (storeName, entities) =>
            openDb([storeName]).then(db => new Promise((resolve, reject) => {
                const t = db.transaction(storeName, 'readwrite');
                const store = t.objectStore(storeName);
                entities.forEach(e => store.put(e));
                t.oncomplete = () => resolve();
                t.onerror = e => reject(e.target.error);
            })),

        subscribeOnline: (dotNetRef) => {
            const handler = () => dotNetRef.invokeMethodAsync('OnOnline');
            window.addEventListener('online', handler);
            return { dispose: () => window.removeEventListener('online', handler) };
        }
    };
})();
