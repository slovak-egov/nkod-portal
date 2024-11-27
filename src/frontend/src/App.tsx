import { AxiosResponse, RawAxiosRequestHeaders } from 'axios';
import { useCallback, useContext, useEffect, useState } from 'react';
import { BrowserRouter, Route, Routes, useNavigate } from 'react-router-dom';
import { Language, LanguageOptionsContext, TokenContext, TokenResult, UserInfo, sendPost, supportedLanguages, useUserInfo } from './client';
import { Footer } from './components/Footer';
import Header from './components/Header';
import ActivateUser from './pages/ActivateUser';
import AddCatalog from './pages/AddCatalog';
import AddDataset from './pages/AddDataset';
import AddDistribution from './pages/AddDistribution';
import AddPublisher from './pages/AddPublisher';
import AddUser from './pages/AddUser';
import ApplicationDetail from './pages/ApplicationDetail';
import ApplicationForm from './pages/ApplicationForm';
import ApplicationList from './pages/ApplicationList';
import CatalogList from './pages/CatalogList';
import ChangeLicenses from './pages/ChangeLicenses';
import Codelists from './pages/Codelists';
import DatasetList from './pages/DatasetList';
import DetailDataset from './pages/DetailDataset';
import DetailLocalCatalog from './pages/DetailLocalCatalog';
import DistributionList from './pages/DistributionList';
import EditCatalog from './pages/EditCatalog';
import EditDataset from './pages/EditDataset';
import EditDistribution from './pages/EditDistribution';
import EditPublisher from './pages/EditPublisher';
import EditUser from './pages/EditUser';
import ForgottenPassword from './pages/ForgottenPassword';
import ForgottenPasswordActivation from './pages/ForgottenPasswordActivation';
import HomePage from './pages/HomePage';
import InfoPageInvalidDelegation from './pages/InfoPageInvalidDelegation';
import InfoPageWaitingForApprove from './pages/InfoPageWaitingForApprove';
import Invitation from './pages/Invitation';
import Login from './pages/Login';
import LoginInProgress from './pages/LoginInProgress';
import NotFound from './pages/NotFound';
import Profile from './pages/Profile';
import PublicDatasetList from './pages/PublicDatasetList';
import PublicLocalCatalogList from './pages/PublicLocalCatalogList';
import PublicPublisherList from './pages/PublicPublisherList';
import PublisherList from './pages/PublisherList';
import PublisherRegistration from './pages/PublisherRegistration';
import Quality from './pages/Quality';
import RegisterUser from './pages/RegisterUser';
import Sparql from './pages/Sparql';
import SuggestionDetail from './pages/SuggestionDetail';
import SuggestionForm from './pages/SuggestionForm';
import SuggestionList from './pages/SuggestionList';
import UserList from './pages/UserList';
import Alert from './components/Alert';

type Props = {
    extenalToken: TokenResult | null;
};

function AppNavigator(props: Props) {
    const ctx = useContext(TokenContext);
    const navigate = useNavigate();
    const [userInfo] = useUserInfo();
    const token = ctx?.token;

    useEffect(() => {
        if (window.location.href.endsWith('/consume')) {
            if (token?.redirectUrl) {
                navigate(token.redirectUrl);
            } else if (userInfo !== null) {
                if (userInfo.publisherView === null) {
                    if (userInfo.role === 'PublisherAdmin') {
                        navigate('/registracia');
                    } else if (userInfo.role === 'Superadmin' || userInfo.role === 'CommunityUser') {
                        navigate('/');
                    } else if (userInfo.role !== 'Superadmin' && userInfo.role !== 'CommunityUser') {
                        navigate('/sprava/neplatne-zastupenie');
                    }
                } else if (!userInfo.publisherActive) {
                    navigate('/sprava/caka-na-schvalenie');
                } else if (userInfo.role === null) {
                    navigate('/sprava/neplatne-zastupenie');
                } else {
                    navigate('/');
                }
            }
        }
    }, [token, userInfo, navigate]);

    return null;
}

const defaultHeaders: RawAxiosRequestHeaders = {};

function App(props: Props) {
    const [token, setToken] = useState(props.extenalToken);
    const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
    const [headers, setHeaders] = useState<RawAxiosRequestHeaders>(defaultHeaders);
    const [userInfoIsLoading, setUserInfoIsLoading] = useState<boolean>(true);
    const [language, setLanguage] = useState<Language>(supportedLanguages[0]);

    useEffect(() => {
        if (token) {
            setHeaders({
                Authorization: `Bearer ${token.token}`
            });
        } else {
            setHeaders(defaultHeaders);
        }
    }, [token]);

    const refreshToken = useCallback(
        async function () {
            try {
                if (token?.refreshToken) {
                    const response: AxiosResponse<TokenResult> = await sendPost(
                        'refresh',
                        {
                            accessToken: token.token,
                            refreshToken: token.refreshToken
                        },
                        headers
                    );
                    if (response.data?.token) {
                        setToken(response.data);
                        if (response.data.refreshTokenInSeconds > 0) {
                            setTimeout(refreshToken, response.data.refreshTokenInSeconds * 1000);
                        }
                    }
                }
            } catch (err) {}
        },
        [token, headers]
    );

    useEffect(() => {
        if (token?.refreshTokenAfter) {
            const refreshTokenAfter = new Date(token.refreshTokenAfter);
            const now = new Date();
            const diff = refreshTokenAfter.getTime() - now.getTime();
            if (diff > 0) {
                const timeout = setTimeout(() => {
                    refreshToken();
                }, diff);

                return () => {
                    clearTimeout(timeout);
                };
            } else {
                refreshToken();
            }
        }
    }, [token, refreshToken]);

    useEffect(() => {
        async function load() {
            try {
                if (!headers['Authorization']) {
                    setUserInfo(null);
                    setUserInfoIsLoading(false);
                    return;
                }

                setUserInfoIsLoading(true);

                setUserInfo((await sendPost('user-info', {}, headers)).data);
            } catch (err) {
                setUserInfo(null);
            } finally {
                setUserInfoIsLoading(false);
            }
        }

        load();
    }, [headers]);

    return (
        <TokenContext.Provider
            value={{
                token: token,
                setToken: setToken,
                defaultHeaders: headers,
                userInfo: userInfo,
                userInfoLoading: userInfoIsLoading
            }}
        >
            <LanguageOptionsContext.Provider
                value={{
                    language: language,
                    setLanguage: setLanguage
                }}
            >
                <BrowserRouter>
                    {window.location.hostname !== 'data.slovensko.sk' ? (
                        <Alert type="warning" style={{ margin: 0, padding: '20px 0' }}>
                            TEST verzia
                        </Alert>
                    ) : null}
                    <Alert style={{ margin: 0, padding: '20px 0' }}>
                        Zúčastnite sa{' '}
                        <a href="https://docs.google.com/forms/d/e/1FAIpQLSfnYVGTOBKyUSBrAvBALwqP8oHJOKlwtsdMTCq-vUKcNdPIzw/viewform?usp=sf_link">
                            prieskumu používateľov portálu
                        </a>{' '}
                        a pomôžte nám zlepšovať otvorené dáta na Slovensku.
                    </Alert>
                    <Header />
                    <AppNavigator {...props} />
                    <div className="govuk-width-container">
                        <div className="govuk-grid-row">
                            <div className="govuk-grid-column-full">
                                <Routes>
                                    <Route path="/" Component={HomePage} />

                                    <Route path="/datasety/:id/komentare" element={<DetailDataset scrollToComments />} />
                                    <Route path="/datasety/:id" element={<DetailDataset />} />
                                    <Route path="/datasety" element={<PublicDatasetList />} />
                                    <Route path="/poskytovatelia" element={<PublicPublisherList />} />
                                    <Route path="/lokalne-katalogy/:id" element={<DetailLocalCatalog />} />
                                    <Route path="/lokalne-katalogy" element={<PublicLocalCatalogList />} />
                                    <Route path="/sparql" Component={Sparql} />
                                    <Route path="/kvalita-metadat" Component={Quality} />

                                    <Route path="/pozvanka" Component={Invitation} />
                                    <Route path="/saml/consume" Component={LoginInProgress} />

                                    {userInfo ? (
                                        <>
                                            <Route path="/sprava/neplatne-zastupenie" Component={InfoPageInvalidDelegation} />
                                            <Route path="/sprava/caka-na-schvalenie" Component={InfoPageWaitingForApprove} />
                                        </>
                                    ) : null}

                                    {userInfo?.publisher && userInfo.publisherView == null ? (
                                        <Route path="/registracia" Component={PublisherRegistration} />
                                    ) : null}

                                    {userInfo?.publisher && userInfo.publisherActive && userInfo.role ? (
                                        <>
                                            <Route path="/sprava/datasety" Component={DatasetList} />
                                            <Route path="/sprava/datasety/pridat" Component={AddDataset} />
                                            <Route path="/sprava/datasety/upravit/:id" Component={EditDataset} />

                                            <Route path="/sprava/distribucie/:datasetId" Component={DistributionList} />
                                            <Route path="/sprava/distribucie/:datasetId/pridat" Component={AddDistribution} />
                                            <Route path="/sprava/distribucie/:datasetId/upravit/:id" Component={EditDistribution} />

                                            <Route path="/sprava/lokalne-katalogy" Component={CatalogList} />
                                            <Route path="/sprava/lokalne-katalogy/pridat" Component={AddCatalog} />
                                            <Route path="/sprava/lokalne-katalogy/upravit/:id" Component={EditCatalog} />

                                            <Route path="/sprava/zmena-licencii" Component={ChangeLicenses} />
                                        </>
                                    ) : null}

                                    {userInfo?.publisher &&
                                    userInfo.publisherActive &&
                                    (userInfo.role === 'PublisherAdmin' || userInfo.role === 'Superadmin') ? (
                                        <>
                                            <Route path="/sprava/pouzivatelia" Component={UserList} />
                                            <Route path="/sprava/pouzivatelia/pridat" Component={AddUser} />
                                            <Route path="/sprava/pouzivatelia/upravit/:id" Component={EditUser} />
                                            <Route path="/sprava/profil" Component={Profile} />
                                        </>
                                    ) : null}

                                    <Route path="/aplikacia" Component={ApplicationList} />
                                    <Route path="/aplikacia/:id/upravit" Component={ApplicationForm} />
                                    <Route path="/aplikacia/:id/komentare" element={<ApplicationDetail scrollToComments />} />
                                    <Route path="/aplikacia/:id" element={<ApplicationDetail />} />
                                    <Route path="/aplikacia/pridat" Component={ApplicationForm} />
                                    <Route path="/podnet" Component={SuggestionList} />
                                    <Route path="/podnet/:id/upravit" element={<SuggestionForm />} />
                                    <Route path="/podnet/:id/komentare" element={<SuggestionDetail scrollToComments />} />
                                    <Route path="/podnet/:id" element={<SuggestionDetail />} />
                                    <Route path="/podnet/pridat" Component={SuggestionForm} />

                                    {userInfo?.role === 'Superadmin' ? (
                                        <>
                                            <Route path="/sprava/poskytovatelia" Component={PublisherList} />
                                            <Route path="/sprava/poskytovatelia/pridat" Component={AddPublisher} />
                                            <Route path="/sprava/poskytovatelia/upravit/:id" Component={EditPublisher} />
                                            <Route path="/sprava/ciselniky" Component={Codelists} />
                                        </>
                                    ) : null}

                                    <Route path="/registracia" Component={RegisterUser} />
                                    <Route path="/prihlasenie" Component={Login} />
                                    <Route path="/potvrdenie-registracie" Component={ActivateUser} />
                                    <Route path="/zabudnute-heslo" Component={ForgottenPassword} />
                                    <Route path="/obnova-hesla" Component={ForgottenPasswordActivation} />

                                    <Route path="*" Component={NotFound} />
                                </Routes>
                            </div>
                        </div>
                    </div>
                    <Footer />
                </BrowserRouter>
            </LanguageOptionsContext.Provider>
        </TokenContext.Provider>
    );
}

export default App;
