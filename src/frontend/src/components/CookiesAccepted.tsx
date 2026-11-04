import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { cookiesSettingsPath, useCookieConsent } from '../cookies';

export default function CookiesAccepted() {
    const { hideConfirmation } = useCookieConsent();
    const messageRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        messageRef.current?.focus();
    }, []);

    return (
        <div className="idsk-cookie-banner" data-nosnippet role="region" aria-label="Cookies na stránke Národného katalógu otvorených dát">
            <div className="idsk-cookie-banner__message govuk-width-container" role="alert" tabIndex={-1} ref={messageRef}>
                <div className="govuk-grid-row">
                    <div className="govuk-grid-column-two-thirds">
                        <div className="idsk-cookie-banner__content">
                            <p>
                                Prijali ste ukladanie analytických cookie súborov. Túto voľbu môžete kedykoľvek zmeniť v{' '}
                                <Link className="govuk-link" to={cookiesSettingsPath} title="nastaveniach">
                                    nastaveniach
                                </Link>
                                .
                            </p>
                        </div>
                    </div>
                </div>

                <div className="idsk-button-group">
                    <button type="button" className="idsk-button" data-module="idsk-button" onClick={hideConfirmation}>
                        Skryť túto správu
                    </button>
                </div>
            </div>
        </div>
    );
}
