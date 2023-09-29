import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import sk from './locales/sk.json';

i18n.use(initReactI18next).init({
    resources: {
        'sk': {
            translation: sk
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