mergeInto(LibraryManager.library, {
    LittleColony_SetLanguage: function (language) {
        if (window.LittleColonyI18n) {
            window.LittleColonyI18n.setLanguage(language);
        }
    }
});
