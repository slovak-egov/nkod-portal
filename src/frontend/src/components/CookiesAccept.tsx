export default function CookiesAccept() {
    return (
        <>
            <form method="POST">
                <div className="idsk-cookie-banner " data-nosnippet role="region" aria-label="Cookies na [názov stránky/služby]">
                    <div className="govuk-width-container">
                        <div className="idsk-cookie-banner__message idsk-cookie-banner__example govuk-width-container">
                            <div className="govuk-grid-row">
                                <div className="govuk-grid-column-two-thirds">
                                    <h2 className="idsk-cookie-banner__heading govuk-heading-m">Cookies na stránke Národného katalógu otvorených dát</h2>

                                    <div className="idsk-cookie-banner__content">
                                        <p>Aby táto stránka fungovala, používame nevyhnutné súbory cookies.</p>
                                        <p>Používame aj analytické cookies – ich prijatím súhlasíte s ich používaním.</p>
                                        <p>
                                            Viac informácií o spracúvaní cookies nájdete v časti{' '}
                                            <a className="govuk-link" href="#">
                                                Spracúvanie cookies na webovej stránke Národného katalógu otvorených dát
                                            </a>
                                            .
                                        </p>
                                    </div>
                                </div>
                            </div>

                            <div className="idsk-button-group">
                                <button value="accept" type="button" name="cookies" className="idsk-button" data-module="idsk-button">
                                    Prijať dodatočné cookies
                                </button>
                                <button value="reject" type="button" name="cookies" className="idsk-button" data-module="idsk-button">
                                    Odmietnuť dodatočné cookies
                                </button>
                                <a className="govuk-link" href="#" title="Nastavenia cookies">
                                    Nastavenia cookies
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </form>
        </>
    );
}
