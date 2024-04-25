import React, { useContext } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Link } from 'react-router-dom';

import Breadcrumbs from '../../components/Breadcrumbs';
import PageHeader from '../../components/PageHeader';
import MainContent from '../../components/MainContent';
import FormElementGroup from '../../components/FormElementGroup';
import BaseInput from '../../components/BaseInput';
import { CmsUserContext, getCmsUser, useCmsUserLogin } from '../../cms';
import Alert from '../../components/Alert';
import ErrorAlert from '../../components/ErrorAlert';
import Button from '../../components/Button';
import GridRow from '../../components/GridRow';
import GridColumn from '../../components/GridColumn';
import idskLogo from '../../assets/images/idsk_favicon.jpg';

export default function Login() {
    const cmsUserContext = useContext(CmsUserContext);
    const navigate = useNavigate();
    const { t } = useTranslation();
    const [user, setUser, genericError, saving, save] = useCmsUserLogin({
        username: '',
        password: ''
    });

    return (
        <>
            <Breadcrumbs
                items={[
                    { title: t('nkod'), link: '/' },
                    {
                        title: t('odCommunity'),
                        link: '/odkomunita'
                    },
                    { title: t('login') }
                ]}
            />
            <MainContent>
                <PageHeader>{t('login')}</PageHeader>
                <h2 className="govuk-heading-m ">{t('homePage.title')}</h2>
                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={2}>
                        <FormElementGroup
                            label={t('userName')}
                            element={(id) => (
                                <BaseInput id={id} disabled={saving} value={user.username} onChange={(e) => setUser({ username: e.target.value })} />
                            )}
                        />
                    </GridColumn>
                </GridRow>
                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={2}>
                        <FormElementGroup
                            label={t('password')}
                            element={(id) => (
                                <BaseInput
                                    type={'password'}
                                    id={id}
                                    disabled={saving}
                                    value={user.password}
                                    onChange={(e) => setUser({ password: e.target.value })}
                                />
                            )}
                        />
                    </GridColumn>
                </GridRow>
                {genericError ? (
                    <Alert type={'warning'}>
                        <ErrorAlert error={genericError} />
                    </Alert>
                ) : null}
                <Button
                    style={{ marginRight: '20px' }}
                    onClick={async () => {
                        const result = await save();
                        cmsUserContext?.setCmsUser(await getCmsUser());
                        if (result?.success) {
                            navigate('/odkomunita/user-page', { state: { info: t('loginSuccessful') } });
                        }
                    }}
                >
                    {t('loginPage.loginButton')}
                </Button>
                <Button
                    style={{ marginRight: '20px' }}
                    buttonType={'secondary'}
                    onClick={() => {
                        setUser({ username: '', password: '' });
                    }}
                >
                    {t('loginPage.cancelButton')}
                </Button>

                <h2 className="govuk-heading-m">{t('loginPage.socialLogin.title')}</h2>

                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={2}>
                        <a
                            href="#"
                            role="button"
                            draggable="false"
                            className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                            data-module="idsk-button"
                        >
                            <img src={idskLogo} alt="eGovernment" height={24} />
                            <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.eGovernment')}</span>
                        </a>
                    </GridColumn>
                </GridRow>
                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={2}>
                        <a
                            href="#"
                            role="button"
                            draggable="false"
                            className="idsk-button idsk-button--start idsk-button idsk-button--secondary  govuk-!-width-full"
                            data-module="idsk-button"
                        >
                            <svg xmlns="http://www.w3.org/2000/svg" height="24" viewBox="0 0 24 24" width="24">
                                <path
                                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                                    fill="#4285F4"
                                />
                                <path
                                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                                    fill="#34A853"
                                />
                                <path
                                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                                    fill="#FBBC05"
                                />
                                <path
                                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                                    fill="#EA4335"
                                />
                                <path d="M1 1h22v22H1z" fill="none" />
                            </svg>
                            <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.google')}</span>
                        </a>
                    </GridColumn>
                </GridRow>
                {false && (
                    <>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={2}>
                                {' '}
                                <a
                                    href="#"
                                    role="button"
                                    draggable="false"
                                    className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                                    data-module="idsk-button"
                                >
                                    <svg xmlns="http://www.w3.org/2000/svg" height="24" viewBox="0 0 24 24" width="24">
                                        <path fill="#f3f3f3" d="M0 0h23v23H0z" />
                                        <path fill="#f35325" d="M1 1h10v10H1z" />
                                        <path fill="#81bc06" d="M12 1h10v10H12z" />
                                        <path fill="#05a6f0" d="M1 12h10v10H1z" />
                                        <path fill="#ffba08" d="M12 12h10v10H12z" />
                                    </svg>
                                    <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.microsoft')}</span>
                                </a>
                            </GridColumn>
                        </GridRow>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={2}>
                                {' '}
                                <a
                                    href="#"
                                    role="button"
                                    draggable="false"
                                    className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                                    data-module="idsk-button"
                                >
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 814 1000">
                                        <path d="M788.1 340.9c-5.8 4.5-108.2 62.2-108.2 190.5 0 148.4 130.3 200.9 134.2 202.2-.6 3.2-20.7 71.9-68.7 141.9-42.8 61.6-87.5 123.1-155.5 123.1s-85.5-39.5-164-39.5c-76.5 0-103.7 40.8-165.9 40.8s-105.6-57-155.5-127C46.7 790.7 0 663 0 541.8c0-194.4 126.4-297.5 250.8-297.5 66.1 0 121.2 43.4 162.7 43.4 39.5 0 101.1-46 176.3-46 28.5 0 130.9 2.6 198.3 99.2zm-234-181.5c31.1-36.9 53.1-88.1 53.1-139.3 0-7.1-.6-14.3-1.9-20.1-50.6 1.9-110.8 33.7-147.1 75.8-28.5 32.4-55.1 83.6-55.1 135.5 0 7.8 1.3 15.6 1.9 18.1 3.2.6 8.4 1.3 13.6 1.3 45.4 0 102.5-30.4 135.5-71.3z" />
                                    </svg>
                                    <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.apple')}</span>
                                </a>
                            </GridColumn>
                        </GridRow>

                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={2}>
                                {' '}
                                <a
                                    href="#"
                                    role="button"
                                    draggable="false"
                                    className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                                    data-module="idsk-button"
                                >
                                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" height="24" width="24">
                                        <defs id="defs4732">
                                            <clipPath clipPathUnits="userSpaceOnUse" id="clipPath4674">
                                                <path d="M 0,500 1024,500 1024,0 0,0 0,500 Z" id="path4676" />
                                            </clipPath>
                                        </defs>
                                        <g transform="translate(-33.466291,-429.48076)" id="layer1">
                                            <g transform="matrix(1.1165523,0,0,-1.1165523,-103.48743,863.08638)" id="g4670">
                                                <g id="g4672" clip-path="url(#clipPath4674)">
                                                    <g id="g4678" transform="translate(375.7163,120.5527)">
                                                        <path
                                                            d="m 0,0 c 8.134,0 14.73,6.596 14.73,14.73 l 0,237.434 c 0,8.137 -6.596,14.731 -14.73,14.731 l -237.433,0 c -8.137,0 -14.73,-6.594 -14.73,-14.731 l 0,-237.434 c 0,-8.134 6.593,-14.73 14.73,-14.73 L 0,0 Z"
                                                            fill="#3b5998"
                                                            fillOpacity={1}
                                                            fillRule={'nonzero'}
                                                            stroke={'none'}
                                                            id="path4680"
                                                        />
                                                    </g>
                                                    <g id="g4682" transform="translate(307.7046,120.5527)">
                                                        <path
                                                            d="m 0,0 0,103.355 34.693,0 5.194,40.28 -39.887,0 0,25.717 c 0,11.662 3.238,19.609 19.962,19.609 l 21.33,0.01 0,36.026 c -3.69,0.49 -16.351,1.587 -31.081,1.587 -30.753,0 -51.806,-18.771 -51.806,-53.244 l 0,-29.705 -34.781,0 0,-40.28 34.781,0 L -41.595,0 0,0 Z"
                                                            fill="#ffffff"
                                                            fillOpacity={1}
                                                            fillRule={'nonzero'}
                                                            stroke={'none'}
                                                            id="path4684"
                                                        />
                                                    </g>
                                                </g>
                                            </g>
                                        </g>
                                    </svg>
                                    <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.facebook')}</span>
                                </a>
                            </GridColumn>
                        </GridRow>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={2}>
                                <a
                                    href="#"
                                    role="button"
                                    draggable="false"
                                    className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                                    data-module="idsk-button"
                                >
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 248 204">
                                        <path
                                            fill="#1d9bf0"
                                            d="M221.95 51.29c.15 2.17.15 4.34.15 6.53 0 66.73-50.8 143.69-143.69 143.69v-.04c-27.44.04-54.31-7.82-77.41-22.64 3.99.48 8 .72 12.02.73 22.74.02 44.83-7.61 62.72-21.66-21.61-.41-40.56-14.5-47.18-35.07 7.57 1.46 15.37 1.16 22.8-.87-23.56-4.76-40.51-25.46-40.51-49.5v-.64c7.02 3.91 14.88 6.08 22.92 6.32C11.58 63.31 4.74 33.79 18.14 10.71c25.64 31.55 63.47 50.73 104.08 52.76-4.07-17.54 1.49-35.92 14.61-48.25 20.34-19.12 52.33-18.14 71.45 2.19 11.31-2.23 22.15-6.38 32.07-12.26-3.77 11.69-11.66 21.62-22.2 27.93 10.01-1.18 19.79-3.86 29-7.95-6.78 10.16-15.32 19.01-25.2 26.16z"
                                        />
                                    </svg>
                                    <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.twitter')}</span>
                                </a>
                            </GridColumn>
                        </GridRow>
                    </>
                )}

                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={4}>
                        <Link to="#" className="idsk-card-title govuk-link">
                            {t('loginPage.problemLink')}
                        </Link>
                    </GridColumn>
                    <GridColumn widthUnits={1} totalUnits={4}>
                        <Link to="/odkomunita/register-user" className="idsk-card-title govuk-link">
                            {t('loginPage.registerLink')}
                        </Link>
                    </GridColumn>
                </GridRow>
            </MainContent>
        </>
    );
}
