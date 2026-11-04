import { useLocation } from 'react-router-dom';
import { cookiesSettingsPath, useCookieConsent } from '../cookies';
import CookiesAccept from './CookiesAccept';
import CookiesAccepted from './CookiesAccepted';
import CookiesNotAccepted from './CookiesNotAccepted';

export default function CookieBanner() {
    const { policy, confirmation } = useCookieConsent();
    const location = useLocation();

    if (confirmation === 'accepted') {
        return <CookiesAccepted />;
    }

    if (confirmation === 'rejected') {
        return <CookiesNotAccepted />;
    }

    // Na stránke s nastaveniami sa lišta nezobrazuje, voľba sa robí priamo vo formulári
    if (policy === null && location.pathname !== cookiesSettingsPath) {
        return <CookiesAccept />;
    }

    return null;
}
