import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import sk from './locales/sk.json';
import en from './locales/en.json';

i18n.use(initReactI18next).init({
    resources: {
        sk: {
            translation: sk
        },
        en: {
            translation: en
        }
    },
    fallbackLng: 'sk',
    lng: 'sk',
    debug: true,
    interpolation: {
        escapeValue: false
    }
});

export default i18n;
