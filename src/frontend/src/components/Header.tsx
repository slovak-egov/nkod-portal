import { useContext, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { LanguageOptionsContext, TokenContext, doLogout, supportedLanguages, useDefaultHeaders, useUserInfo } from '../client';
import Button from './Button';
import IdSkModule from './IdSkModule';

type MenuItem = {
    title: string;
    link: string;
    submenu: MenuItem[];
};

export default function Header() {
    const tokenContext = useContext(TokenContext);
    const [userInfo, userInfoLoading] = useUserInfo();
    const headers = useDefaultHeaders();

    const navigate = useNavigate();
    const { t, i18n } = useTranslation();
    const l = useContext(LanguageOptionsContext);

    const menu: MenuItem[] = [
        {
            title: t('search'),
            link: '/datasety',
            submenu: []
        },
        {
            title: t('publishers'),
            link: '/poskytovatelia',
            submenu: []
        },
        {
            title: t('localCatalogs'),
            link: '/lokalne-katalogy',
            submenu: []
        },
        {
            title: 'SPARQL',
            link: '/sparql',
            submenu: []
        },
        {
            title: t('metadataQuality'),
            link: '/kvalita-metadat',
            submenu: []
        },
        {
            title: t('application'),
            link: '/aplikacia',
            submenu: []
        },
        {
            title: t('suggestion'),
            link: '/podnet',
            submenu: []
        }
    ];

    const adminMenu: MenuItem[] = [];

    if (userInfo?.publisher && userInfo.role && userInfo.publisherActive) {
        adminMenu.push(
            {
                title: t('datasets'),
                link: '/sprava/datasety',
                submenu: []
            },
            {
                title: t('localCatalogs'),
                link: '/sprava/lokalne-katalogy',
                submenu: []
            }
        );

        if (userInfo?.role === 'PublisherAdmin' || userInfo?.role === 'Superadmin') {
            adminMenu.push(
                {
                    title: t('users'),
                    link: '/sprava/pouzivatelia',
                    submenu: []
                },
                {
                    title: t('profile'),
                    link: '/sprava/profil',
                    submenu: []
                }
            );
        }
    }

    if (userInfo?.role === 'Superadmin') {
        adminMenu.push(
            {
                title: t('publishers'),
                link: '/sprava/poskytovatelia',
                submenu: []
            },
            {
                title: t('codelists.label'),
                link: '/sprava/ciselniky',
                submenu: []
            }
        );
    }

    if (adminMenu.length > 0) {
        menu.push({
            title: t('administration'),
            link: '#1',
            submenu: adminMenu
        });
    }

    const logout = async () => {
        const url = await doLogout(headers);
        tokenContext?.setToken(null);
        if (url) {
            window.location.href = url;
        }
    };

    const navigateToProfile = async () => {
        navigate('/sprava/profil');
    };

    const idPrefix = useMemo(() => {
        return Math.random() * 100000 + (userInfo?.id.length ?? 0);
    }, [userInfo]);

    return (
        <header className="idsk-header-web">
            <IdSkModule moduleType="idsk-header-web" userInfo={userInfo ?? undefined}>
                <div className="idsk-header-web__scrolling-wrapper">
                    <div className="idsk-header-web__tricolor"></div>

                    <div className="idsk-header-web__brand idsk-header-web__brand--light">
                        <div className="govuk-width-container">
                            <div className="govuk-grid-row">
                                <div className="govuk-grid-column-full">
                                    <div className="idsk-header-web__brand-gestor">
                                        <span className="govuk-body-s idsk-header-web__brand-gestor-text">{t('nkod')}</span>
                                    </div>
                                    <div className="idsk-header-web__brand-spacer"></div>
                                    <div className="idsk-header-web__brand-language">
                                        <button
                                            key={idPrefix + '_lang_button'}
                                            className="idsk-header-web__brand-language-button"
                                            aria-label={t('expandLanguageMenu')}
                                            aria-expanded="false"
                                            data-text-for-hide={t('hideLanguageMenu')}
                                            data-text-for-show={t('expandLanguageMenu')}
                                        >
                                            {l?.language.name}
                                        </button>
                                        <ul className="idsk-header-web__brand-language-list">
                                            {supportedLanguages
                                                .filter((lg) => lg.id !== l?.language.id)
                                                .map((lg) => (
                                                    <li key={idPrefix + lg.id} className="idsk-header-web__brand-language-list-item">
                                                        <a
                                                            className="govuk-link idsk-header-web__brand-language-list-item-link"
                                                            title={lg.name}
                                                            href="/"
                                                            onClick={(e) => {
                                                                e.preventDefault();
                                                                if (lg.id) {
                                                                    l?.setLanguage(lg);
                                                                    i18n.changeLanguage(lg.id);
                                                                }
                                                                return false;
                                                            }}
                                                            lang={lg.id}
                                                        >
                                                            {lg.name}
                                                        </a>
                                                    </li>
                                                ))}
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
                                        <Link to="/" title={t('homePageLink')}>
                                            <h2 className="govuk-heading-m">data.slovensko.sk</h2>
                                        </Link>
                                        <button
                                            className="idsk-button idsk-header-web__main-headline-menu-button"
                                            aria-label={t('expandMenu')}
                                            aria-expanded="false"
                                            data-text-for-show={t('expandMenu')}
                                            data-text-for-hide={t('hideMenu')}
                                            data-text-for-close={t('hideMenu')}
                                        >
                                            Menu
                                            <span className="idsk-header-web__menu-open"></span>
                                            <span className="idsk-header-web__menu-close"></span>
                                        </button>
                                    </div>
                                </div>
                                <div className="govuk-grid-column-two-thirds">
                                    <div className="idsk-header-web__main-action">
                                        <div className="idsk-header-web__main--buttons">
                                            {!userInfoLoading ? (
                                                <>
                                                    <div
                                                        className={'idsk-header-web__main--login ' + (userInfo ? 'idsk-header-web__main--login--loggedIn' : '')}
                                                    >
                                                        <Button className="idsk-header-web__main--login-loginbtn" onClick={() => navigate('/prihlasenie')}>
                                                            {t('header.login')}
                                                        </Button>
                                                        <Button
                                                            className="idsk-header-web__main--login-loginbtn"
                                                            buttonType="secondary"
                                                            onClick={() => navigate('/registracia')}
                                                        >
                                                            {t('header.register')}
                                                        </Button>
                                                        <div className="idsk-header-web__main--login-action">
                                                            <div className="idsk-header-web__main--login-action-text">
                                                                <span className="govuk-body-s idsk-header-web__main--login-action-text-user-name">
                                                                    {userInfo?.firstName} {userInfo?.lastName}
                                                                </span>
                                                                {userInfo?.publisherView ? (
                                                                    <div className="govuk-body-s" style={{ margin: 0 }}>
                                                                        {userInfo.publisherView.name}
                                                                    </div>
                                                                ) : null}
                                                                <div className="govuk-!-margin-bottom-1">
                                                                    <a
                                                                        className="govuk-link idsk-header-web__main--login-action-text-logout idsk-header-web__main--login-logoutbtn"
                                                                        href="#"
                                                                        onClick={(e) => {
                                                                            e.preventDefault();
                                                                            logout();
                                                                        }}
                                                                        title={t('logout')}
                                                                    >
                                                                        {t('logout')}
                                                                    </a>
                                                                    {userInfo?.publisherActive ?? false ? (
                                                                        <>
                                                                            <span> | </span>
                                                                            <a
                                                                                className="govuk-link idsk-header-web__main--login-action-text-profile idsk-header-web__main--login-profilebtn"
                                                                                href="#"
                                                                                onClick={(e) => {
                                                                                    e.preventDefault();
                                                                                    navigateToProfile();
                                                                                }}
                                                                                title={t('profile')}
                                                                            >
                                                                                {t('profile')}
                                                                            </a>
                                                                        </>
                                                                    ) : null}
                                                                </div>
                                                            </div>
                                                        </div>
                                                        {/* <Button className="idsk-header-web__main--login-profilebtn">
                                            Profil
                                        </Button>
                                        <Button className="idsk-header-web__main--login-logoutbtn">
                                            Odhlásiť sa
                                        </Button> */}
                                                    </div>
                                                </>
                                            ) : null}
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
                                        <ul className="idsk-header-web__nav-list" aria-label={t('mainNavigation')}>
                                            {menu.map((i) => {
                                                const ariaAttributes: { [id: string]: string } = {};

                                                if (i.submenu.length > 0) {
                                                    ariaAttributes['aria-label'] = t('expandMenu') + ' ' + i.title;
                                                    ariaAttributes['aria-expanded'] = 'false';
                                                    ariaAttributes['data-text-for-hide'] = t('hideMenu') + ' ' + i.title;
                                                    ariaAttributes['data-text-for-show'] = t('expandMenu') + ' ' + i.title;
                                                }

                                                return (
                                                    <li key={idPrefix + i.link} className="idsk-header-web__nav-list-item">
                                                        {i.submenu.length > 0 ? (
                                                            <a
                                                                href="#"
                                                                className="govuk-link idsk-header-web__nav-list-item-link"
                                                                title={i.title}
                                                                {...ariaAttributes}
                                                            >
                                                                {i.title}
                                                                {i.submenu.length > 0 ? (
                                                                    <>
                                                                        <div className="idsk-header-web__link-arrow"></div>
                                                                        <div className="idsk-header-web__link-arrow-mobile"></div>
                                                                    </>
                                                                ) : null}
                                                            </a>
                                                        ) : (
                                                            <Link
                                                                to={i.link}
                                                                className="govuk-link idsk-header-web__nav-list-item-link"
                                                                title={i.title}
                                                                {...ariaAttributes}
                                                            >
                                                                {i.title}
                                                                {i.submenu.length > 0 ? (
                                                                    <>
                                                                        <div className="idsk-header-web__link-arrow"></div>
                                                                        <div className="idsk-header-web__link-arrow-mobile"></div>
                                                                    </>
                                                                ) : null}
                                                            </Link>
                                                        )}
                                                        {i.submenu.length > 0 ? (
                                                            <>
                                                                <div className="idsk-header-web__nav-submenu">
                                                                    <div className="govuk-width-container">
                                                                        <div className="govuk-grid-row">
                                                                            <ul className="idsk-header-web__nav-submenu-list" aria-label={t('innerNavigation')}>
                                                                                {i.submenu.map((s) => (
                                                                                    <li
                                                                                        key={idPrefix + s.link}
                                                                                        className="idsk-header-web__nav-submenu-list-item"
                                                                                    >
                                                                                        <Link
                                                                                            className="govuk-link idsk-header-web__nav-submenu-list-item-link"
                                                                                            to={s.link}
                                                                                            title={s.title}
                                                                                        >
                                                                                            <span>{s.title}</span>
                                                                                        </Link>
                                                                                    </li>
                                                                                ))}
                                                                            </ul>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            </>
                                                        ) : null}
                                                    </li>
                                                );
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
    );
}
