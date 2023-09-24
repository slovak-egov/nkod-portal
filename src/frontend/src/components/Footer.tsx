import { Link } from "react-router-dom";

export function Footer()
{
    return <div data-module="idsk-footer-extended">
        <footer className="idsk-footer-extended  ">
            <div className="idsk-footer-extended-main-content">
                <div className="govuk-width-container">
                    <div className="govuk-grid-column-full">
                        <div className="idsk-footer-extended-description-panel">
                            <div className="govuk-grid-column-two-thirds idsk-footer-extended-info-links">
                                <div className="idsk-footer-extended-meta-item">
                                    <ul className="idsk-footer-extended-inline-list ">
                                        <li className="idsk-footer-extended-inline-list-item">
                                            <Link to="/cookies" className="govuk-link" title="Cookies">
                                                Cookies
                                            </Link>
                                        </li>
                                        <li className="idsk-footer-extended-inline-list-item">
                                            <Link to="/privacy" className="govuk-link" title="Ochrana osobných údajov">
                                                Kontakty
                                            </Link>
                                        </li>
                                    </ul>
                                </div>
                                <p className="idsk-footer-extended-frame">Vytvorené v súlade s&nbsp;
                                    <a className="govuk-link" title="Jednotným dizajn manuálom elektronických služieb." href="https://idsk.gov.sk/">
                                        Jednotným dizajn manuálom elektronických služieb
                                    </a>.
                                </p>
                                <p className="idsk-footer-extended-frame">
                                    Prevádzkovateľom služby je Ministerstvo investícií, regionálneho rozvoja a informatizácie SR.
                                </p>
                            </div>
                            <div className="govuk-grid-column-one-third idsk-footer-extended-logo-box">
                                <p> 
                                    <span>Národný katalóg otvorených dát je súčasťou projektu</span> <a href="https://www.mirri.gov.sk/projekty/projekty-esif/operacny-program-integrovana-infrastruktura/prioritna-os-7-informacna-spolocnost/projekty/otvorene-udaje-2-0/index.html" target="_blank" rel="noopener noreferrer" title="Otvorené údaje 2.0 - Rozvoj centrálnych komponentov pre kvalitné zabezpečenie otvorených údajov"> Otvorené údaje 2.0 - Rozvoj centrálnych komponentov pre kvalitné zabezpečenie otvorených údajov </a> 
                                </p> 
                                <img className="idsk-footer-extended-logo" src="/assets/eulogo.png" alt="EURÓPSKA ÚNIA Európske štrukturálne a investičné fondy OP Integrovaná infraštruktúra 2014 – 2020" /> 
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </footer>
    </div>
}