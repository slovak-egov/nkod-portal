import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import sk from './locales/sk.json';
import en from './locales/en.json';
import de from './locales/de.json';

i18n.use(initReactI18next).init({
    resources: {
        'sk': {
            translation: sk
        },
        'en': {
            translation: en
        },
        'de': {
            translation: de
        }
    },
    fallbackLng: 'sk',
    lng: 'sk',
    debug: true,
    interpolation: {
        escapeValue: false,
    },
});

export default i18n;