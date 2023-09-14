import { Link } from "react-router-dom";
import profileImage from '../assets/images/header-web/profile.svg';

export default function Header() {
    return <header className="idsk-header-web " data-module="idsk-header-web">
        <div className="idsk-header-web__scrolling-wrapper">
            <div className="idsk-header-web__tricolor"></div>
            <div className="idsk-header-web__brand ">
                <div className="govuk-width-container">
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full">
                            <div className="idsk-header-web__brand-gestor">
                                <span className="govuk-body-s idsk-header-web__brand-gestor-text">
                                    Národný katalóg otvorených dát
                                </span>
                                <div className="idsk-header-web__brand-dropdown">
                                    <div className="govuk-width-container">

                                    </div>
                                </div>
                            </div>
                            <div className="idsk-header-web__brand-spacer"></div>
                            <div className="idsk-header-web__brand-language">
                                <button className="idsk-header-web__brand-language-button" aria-label="Rozbaliť jazykové menu" aria-expanded="false" data-text-for-hide="Skryť jazykové menu" data-text-for-show="Rozbaliť jazykové menu">
                                    Slovenčina
                                    <span className="idsk-header-web__link-arrow"></span>
                                </button>
                                {/* <ul className="idsk-header-web__brand-language-list">
                                <   li className="idsk-header-web__brand-language-list-item">
                                        <a className="govuk-link idsk-header-web__brand-language-list-item-link "
                                            title="Deutsch"
                                            href="#">
                                            Deutsch
                                        </a>
                                    </li>
                                </ul> */}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div className="idsk-header-web__main">
                <div className="govuk-width-container">
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full govuk-grid-column-one-third-from-desktop">
                            <div className="idsk-header-web__main-headline">
                                <Link to="/" title="Odkaz na úvodnú stránku">
                                    <h2 className="govuk-heading-m">data.gov.sk</h2>
                                </Link>
                                <button className="idsk-button idsk-header-web__main-headline-menu-button idsk-header-web__main-headline-menu-button-service" aria-label="Rozbaliť menu" aria-expanded="false" data-text-for-show="Rozbaliť menu" data-text-for-hide="Zavrieť menu">
                                    <img src="/assets/images/header-web//profile.svg" alt="Electronic service menu icon" />
                                    <span className="idsk-header-web__menu-close"></span>
                                </button>
                            </div>
                        </div>
                        <div className="govuk-grid-column-two-thirds">
                            <div className="idsk-header-web__main-action">
                                <div className="idsk-header-web__main--buttons">
                                    <div className="idsk-header-web__main--login ">
                                    {/* <button type="button" className="idsk-button idsk-header-web__main--login-loginbtn" data-module="idsk-button">
  Prihlásiť sa
</button> */}
                                        {/* <div className="idsk-header-web__main--login-action" style={{display: 'flex'}}>
                                            <img className="idsk-header-web__main--login-action-profile-img" src={profileImage} alt="Profile image" />
                                            <div className="idsk-header-web__main--login-action-text">
                                                <span className="govuk-body-s idsk-header-web__main--login-action-text-user-name">
                                                    Ing. Jožko Veľký M.A
                                                </span>
                                                <div className="govuk-!-margin-bottom-1">
                                                    <a className="govuk-link idsk-header-web__main--login-action-text-logout idsk-header-web__main--login-logoutbtn" href="#" title="odhlásiť" style={{display: 'inline'}}>
                                                    Odhlásiť
                                                    </a>
                                                </div>
                                            </div>
                                        </div>
                                        <button type="button" className="idsk-button idsk-header-web__main--login-profilebtn" data-module="idsk-button">
                                            Profil
                                        </button>
                                            <button type="button" className="idsk-button idsk-header-web__main--login-logoutbtn" data-module="idsk-button">
                                            Odhlásiť sa
                                        </button> */}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div className="idsk-header-web__nav idsk-header-web__nav--mobile ">
                <div className="govuk-width-container">
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full"></div>
                    </div>
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full">
                            <div className="idsk-header-web__main--buttons">
                                <div className="idsk-header-web__main--login ">
                                    {/* <button type="button" className="idsk-button idsk-header-web__main--login-loginbtn"
                                        data-module="idsk-button">
                                        Prihlásiť sa
                                    </button> */}
                                    <div className="idsk-header-web__main--login-action">
                                        {/* <img className="idsk-header-web__main--login-action-profile-img"
                                            src="/assets/images/header-web/profile.svg" alt="Profile image" />
                                        <div className="idsk-header-web__main--login-action-text">
                                            <span className="govuk-body-s idsk-header-web__main--login-action-text-user-name">
                                            Ing. Jožko Veľký M.A
                                            </span>
                                            <div className="govuk-!-margin-bottom-1">
                                            <a className="govuk-link idsk-header-web__main--login-action-text-logout idsk-header-web__main--login-logoutbtn"
                                                href="#" title="odhlásiť">
                                                Odhlásiť
                                            </a>
                                            <span> | </span>
                                            <a className="govuk-link idsk-header-web__main--login-action-text-profile idsk-header-web__main--login-profilebtn"
                                                href="#" title="profil">
                                                Profil
                                            </a>
                                            </div>
                                        </div> */}
                                    </div>
                                    {/* <button type="button" className="idsk-button idsk-header-web__main--login-profilebtn"
                                        data-module="idsk-button">
                                        Profil
                                    </button>
                                    <button type="button" className="idsk-button idsk-header-web__main--login-logoutbtn"
                                        data-module="idsk-button">
                                        Odhlásiť sa
                                    </button> */}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div className="idsk-header-web__nav--divider"></div>
<div className="idsk-header-web__nav idsk-header-web__nav--mobile ">
  <div className="govuk-width-container">
    <div className="govuk-grid-row">
      <div className="govuk-grid-column-full">
        
      </div>
        <div className="govuk-grid-column-full">
          <nav className="idsk-header-web__nav-bar--buttons">
            <ul className="idsk-header-web__nav-list "
               aria-label="Hlavná navigácia">
                <li className="idsk-header-web__nav-list-item">
                  <Link className="govuk-link idsk-header-web__nav-list-item-link" to="/datasety"  title="Vyhľadávanie"  aria-label="Rozbaliť Vyhľadávanie menu" aria-expanded="false"
                    data-text-for-hide="Zavrieť Vyhľadávanie menu" data-text-for-show="Rozbaliť Vyhľadávanie menu" >
                    Vyhľadávanie
                  </Link>
                </li>
                <li className="idsk-header-web__nav-list-item">
                  <Link className="govuk-link idsk-header-web__nav-list-item-link" to="/poskytovatelia"  title="Poskytovatelia"  aria-label="Rozbaliť Poskytovatelia menu" aria-expanded="false"
                    data-text-for-hide="Zavrieť Poskytovatelia menu" data-text-for-show="Rozbaliť Poskytovatelia menu" >
                    Poskytovatelia
                  </Link>
                </li>
                <li className="idsk-header-web__nav-list-item">
                  <Link className="govuk-link idsk-header-web__nav-list-item-link" to="/lokalne-katalogy"  title="Lokálne katalógy" aria-label="Rozbaliť Lokálne katalógy menu" aria-expanded="false"
                    data-text-for-hide="Zavrieť Lokálne katalógy menu" data-text-for-show="Rozbaliť Lokálne katalógy menu">
                    Lokálne katalógy
                  </Link>
                </li>
            </ul>
        </nav>
      </div>
  </div></div></div>

    </header>
}