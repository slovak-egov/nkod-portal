import { Link, useNavigate } from "react-router-dom";
import { doLogin, doLogout, useDefaultHeaders, useUserInfo } from "../client";
import Button from "./Button";
import IdSkModule from "./IdSkModule";

type MenuItem = {
    title: string;
    link: string;
    submenu: MenuItem[];
}

export default function Header() {
    const [ userInfo, userInfoLoading ] = useUserInfo();
    const headers = useDefaultHeaders();

    const navigate = useNavigate();

    const menu : MenuItem[] = [
        {
            title: 'Vyhľadávanie',
            link: '/datasety',
            submenu: []
        },
        {
            title: 'Poskytovatelia',
            link: '/poskytovatelia',
            submenu: []
        },
        {
            title: 'Lokálne katalógy',
            link: '/lokalne-katalogy',
            submenu: []
        },
        {
            title: 'SPARQL',
            link: '/sparql',
            submenu: []
        },
        {
            title: 'Kvalita metadát',
            link: '/kvalita-metadat',
            submenu: []
        }
    ];

    const adminMenu : MenuItem[] = [];

    if (userInfo?.publisher) {
        adminMenu.push({
            title: 'Datasety',
            link: '/sprava/datasety',
            submenu: []
        },
        {
            title: 'Lokálne katalógy',
            link: '/sprava/lokalne-katalogy',
            submenu: []
        });
    }

    if (userInfo?.role === 'PublisherAdmin') {
        adminMenu.push({
            title: 'Používatelia',
            link: '/sprava/pouzivatelia',
            submenu: []
        }, {
            title: 'Profil',
            link: '/sprava/profil',
            submenu: []
        });
    }

    if (userInfo?.role === 'Superadmin') {
        adminMenu.push({
            title: 'Poskytovatelia',
            link: '/sprava/poskytovatelia',
            submenu: []
        },{
            title: 'Číselníky',
            link: '/sprava/ciselniky',
            submenu: []
        });
    }

    if (adminMenu.length > 0) {
        menu.push({
            title: 'Správa',
            link: adminMenu[0].link,
            submenu: adminMenu
        });
    }

    const login = async () => {
        const url = await doLogin(headers);
        if (url) {
            window.location.href = url;
        }
    };

    const logout = async () => {
        await doLogout(headers);
        navigate('/');
    }

    const navigateToProfile = async () => {
        navigate('/sprava/profil');
    }

    return <header className="idsk-header-web">
        <IdSkModule moduleType="idsk-header-web">
        <div className="idsk-header-web__scrolling-wrapper">
            
            <div className="idsk-header-web__tricolor"></div>

            <div className="idsk-header-web__brand idsk-header-web__brand--light">
                <div className="govuk-width-container">
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full">
                            <div className="idsk-header-web__brand-gestor">
                                <span className="govuk-body-s idsk-header-web__brand-gestor-text">
                                    Národný katalóg otvorených dát
                                </span>
                            </div>
                            <div className="idsk-header-web__brand-spacer"></div>
                            <div className="idsk-header-web__brand-language">
                                <button className="idsk-header-web__brand-language-button" aria-label="Rozbaliť jazykové menu" aria-expanded="false" data-text-for-hide="Skryť jazykové menu" data-text-for-show="Rozbaliť jazykové menu">
                                    Slovenčina
                                </button>
                                <ul className="idsk-header-web__brand-language-list">
                                    <li className="idsk-header-web__brand-language-list-item">
                                        {/* {{ 'idsk-header-web__brand-language-list-item-link--selected' if language.selected }} */}
                                        <a className="govuk-link idsk-header-web__brand-language-list-item-link"  
                                            title="English"
                                            href="/"
                                            lang="en"
                                            >
                                            English
                                        </a>
                                    </li>
                                </ul>
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
                                <button className="idsk-button idsk-header-web__main-headline-menu-button" aria-label="Rozbaliť menu" aria-expanded="false" data-text-for-show="Rozbaliť menu" data-text-for-hide="Zavrieť menu" data-text-for-close="Zavrieť">
                                    <span className="idsk-header-web__menu-open"></span>
                                    <span className="idsk-header-web__menu-close"></span>
                                </button>
                            </div>
                        </div>
                        <div className="govuk-grid-column-two-thirds">
                            <div className="idsk-header-web__main-action">

                                <div className="idsk-header-web__main--buttons">
                                    {!userInfoLoading ? <><div className={'idsk-header-web__main--login ' + (userInfo ? 'idsk-header-web__main--login--loggedIn' : '')}>
                                        <Button className="idsk-header-web__main--login-loginbtn" onClick={login}>
                                            Prihlásiť sa
                                        </Button>
                                        <div className="idsk-header-web__main--login-action">
                                            <div className="idsk-header-web__main--login-action-text">
                                                <span className="govuk-body-s idsk-header-web__main--login-action-text-user-name">
                                                    {userInfo?.firstName} {userInfo?.lastName}
                                                </span>
                                                <div className="govuk-!-margin-bottom-1">
                                                    <a className="govuk-link idsk-header-web__main--login-action-text-logout idsk-header-web__main--login-logoutbtn" href="#" onClick={e => {e.preventDefault(); logout();}} title="odhlásiť">
                                                        Odhlásiť
                                                    </a>
                                                    <span> | </span>
                                                    <a className="govuk-link idsk-header-web__main--login-action-text-profile idsk-header-web__main--login-profilebtn" href="#" onClick={e => {e.preventDefault(); navigateToProfile();}} title="profil">
                                                        Profil
                                                    </a>
                                                </div>
                                            </div>
                                        </div>
                                        {/* <Button className="idsk-header-web__main--login-profilebtn">
                                            Profil
                                        </Button>
                                        <Button className="idsk-header-web__main--login-logoutbtn">
                                            Odhlásiť sa
                                        </Button> */}
                                    </div></> : null}
                                </div>

                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div className="idsk-header-web__nav--divider"></div>
            {/* {{ 'idsk-header-web__nav--dark' if params.navBar.darkBackground }} */}
            <div className="idsk-header-web__nav idsk-header-web__nav--mobile">
                <div className="govuk-width-container">
                    <div className="govuk-grid-row">
                        <div className="govuk-grid-column-full">                            
                            <nav className="idsk-header-web__nav-bar--buttons">
                                <ul className="idsk-header-web__nav-list" aria-label="Hlavná navigácia">
                                    {menu.map((i) => {
                                        const ariaAttributes: {[id: string]: string} = {};

                                        if (i.submenu.length > 0) {
                                            ariaAttributes["aria-label"] = "Rozbaliť menu " + i.title;
                                            ariaAttributes["aria-expanded"] = "false";
                                            ariaAttributes["data-text-for-hide"] = "Skryť menu " + i.title;
                                            ariaAttributes["data-text-for-show"] = "Rozbaliť menu " + i.title;
                                        }

                                        return <li key={i.link} className="idsk-header-web__nav-list-item">
                                            <Link to={i.link} className="govuk-link idsk-header-web__nav-list-item-link" title={i.title} {...ariaAttributes}>
                                                {i.title}
                                                {i.submenu.length > 0 ? <>
                                                    <div className="idsk-header-web__link-arrow"></div>
                                                    <div className="idsk-header-web__link-arrow-mobile"></div>
                                                </> : null}
                                            </Link>
                                            {i.submenu.length > 0 ? <>
                                                <div className="idsk-header-web__nav-submenu">
                                                    <div className="govuk-width-container">
                                                        <div className="govuk-grid-row">
                                                            <ul className="idsk-header-web__nav-submenu-list"  aria-label="Vnútorná navigácia">
                                                                {i.submenu.map((s) => <li key={s.link} className="idsk-header-web__nav-submenu-list-item">
                                                                    <Link className="govuk-link idsk-header-web__nav-submenu-list-item-link" to={s.link} title={s.title}>
                                                                        <span>{s.title}</span>
                                                                    </Link>
                                                                </li>)}
                                                            </ul>
                                                        </div>
                                                    </div>
                                                </div>
                                            </> : null}
                                        </li>
                                    })}
                                </ul>
                            </nav>
                        </div>                        
                    </div>
                </div>
            </div>


        </div>
        </IdSkModule>
      </header>
    
    
    
    
    
    
    
    
    
    
    
    
    
}