import { Link } from 'react-router-dom';
import { cookiesProcessingPath, cookiesSettingsPath, useCookieConsent } from '../cookies';

export default function CookiesAccept() {
    const { savePolicy } = useCookieConsent();

    return (
        <div className="idsk-cookie-banner" data-nosnippet role="region" aria-label="Cookies na stránke Národného katalógu otvorených dát">
            <div className="idsk-cookie-banner__message govuk-width-container">
                <div className="govuk-grid-row">
                    <div className="govuk-grid-column-two-thirds">
                        <h2 className="idsk-cookie-banner__heading govuk-heading-m">Cookies na stránke Národného katalógu otvorených dát</h2>

                        <div className="idsk-cookie-banner__content">
                            <p>Aby táto stránka fungovala, používame nevyhnutné súbory cookies.</p>
                            <p>Používame aj analytické cookies – ich prijatím súhlasíte s ich používaním.</p>
                            <p>
                                Viac informácií o spracúvaní cookies nájdete v časti{' '}
                                <Link className="govuk-link" to={cookiesProcessingPath}>
                                    Spracúvanie cookies na webovej stránke Národného katalógu otvorených dát
                                </Link>
                                .
                            </p>
                        </div>
                    </div>
                </div>

                <div className="idsk-button-group">
                    <button type="button" className="idsk-button" data-module="idsk-button" onClick={() => savePolicy(true, true)}>
                        Prijať analytické cookies
                    </button>
                    <button type="button" className="idsk-button" data-module="idsk-button" onClick={() => savePolicy(false, true)}>
                        Odmietnuť analytické cookies
                    </button>
                    <Link className="govuk-link" to={cookiesSettingsPath} title="Nastavenia cookies">
                        Nastavenia cookies
                    </Link>
                </div>
            </div>
        </div>
    );
}
