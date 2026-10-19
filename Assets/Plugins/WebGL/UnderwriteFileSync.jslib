// 웹 빌드의 저장 파일(Application.persistentDataPath)은 브라우저 IndexedDB에 있다.
// 쓴 뒤 바로 IndexedDB로 밀어 넣지 않으면 탭을 닫을 때 마지막 저장을 잃을 수 있다.
mergeInto(LibraryManager.library, {
    UnderwriteSyncFiles: function () {
        FS.syncfs(false, function (err) {
            if (err) {
                console.warn("Underwrite: 저장을 브라우저에 넘기지 못했습니다", err);
            }
        });
    }
});
