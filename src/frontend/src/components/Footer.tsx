import { useTranslation } from 'react-i18next';
import { useUserInfo } from '../client';

export function Footer() {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();

    return (
        <div data-module="idsk-footer-extended">
            <footer className="idsk-footer-extended  ">
                <div className="idsk-footer-extended-main-content">
                    <div className="govuk-width-container">
                        <div className="govuk-grid-column-full">
                            <div className="idsk-footer-extended-description-panel">
                                <div className="govuk-grid-column-two-thirds idsk-footer-extended-info-links">
                                    <div className="idsk-footer-extended-meta-item"></div>
                                    <p className="idsk-footer-extended-frame">
                                        {t('createdWith')}&nbsp;
                                        <a className="govuk-link" title={t('idsk')} href="https://idsk.gov.sk/">
                                            {t('idsk')}
                                        </a>
                                        .
                                    </p>
                                    <p className="idsk-footer-extended-frame">{t('websiteOwner')}</p>
                                    <p>
                                        <a
                                            className="govuk-link"
                                            title={t('technicalSupport')}
                                            href="https://wiki.vicepremier.gov.sk/display/opendata/Podpora+pre+data.slovensko.sk"
                                        >
                                            {t('technicalSupport')}
                                        </a>
                                    </p>
                                    {userInfo ? (
                                        <p>
                                            <a className="govuk-link" title={t('notificationSettings')} href="/nastavenie-upozorneni">
                                                {t('notificationSettings')}
                                            </a>
                                        </p>
                                    ) : null}
                                </div>
                                <div className="govuk-grid-column-one-third idsk-footer-extended-logo-box">
                                    <p>
                                        <span>{t('nkodPart')}</span>{' '}
                                        <a
                                            href="https://www.mirri.gov.sk/projekty/projekty-esif/operacny-program-integrovana-infrastruktura/prioritna-os-7-informacna-spolocnost/projekty/otvorene-udaje-2-0/index.html"
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            title={t('openData20')}
                                        >
                                            {' '}
                                            {t('openData20')}{' '}
                                        </a>
                                    </p>
                                    <img className="idsk-footer-extended-logo" src="/assets/eulogo.png" alt={t('euProjectFinancing')} />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </footer>
        </div>
    );
}
