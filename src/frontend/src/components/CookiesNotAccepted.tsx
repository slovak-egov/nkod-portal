export default function CookiesNotAccepted() {
    return (
        <>
            <form method="POST">
                <div className="idsk-cookie-banner " data-nosnippet role="region" aria-label="Cookies na [názov stránky/služby]">
                    <div className="idsk-cookie-banner__message idsk-cookie-banner__example govuk-width-container">
                        <div className="govuk-grid-row">
                            <div className="govuk-grid-column-two-thirds">
                                <div className="idsk-cookie-banner__content">
                                    <p>
                                        Odmietli ste dodatočné súbory cookies. Túto voľbu môžete kedykoľvek zmeniť v{' '}
                                        <a className="govuk-link" href="#" title="nastaveniach">
                                            nastaveniach
                                        </a>
                                        .
                                    </p>
                                </div>
                            </div>
                        </div>

                        <div className="idsk-button-group">
                            <a href="#" role="button" draggable="false" className="idsk-button" data-module="idsk-button">
                                Skryť túto správu
                            </a>
                        </div>
                    </div>
                </div>
            </form>
        </>
    );
}
