import Header from './components/Header';
import {BrowserRouter, Route, Routes, useNavigate} from 'react-router-dom';
import {Footer} from './components/Footer';
import AddDataset from './pages/AddDataset';
import DatasetList from './pages/DatasetList';
import HomePage from './pages/HomePage';
import DetailDataset from './pages/DetailDataset';
import PublicPublisherList from './pages/PublicPublisherList';
import PublicLocalCatalogList from './pages/PublicLocalCatalogList';
import DetailLocalCatalog from './pages/DetailLocalCatalog';
import PublicDatasetList from './pages/PublicDatasetList';
import Alert from './components/Alert';
import EditDataset from './pages/EditDataset';
import DistributionList from './pages/DistributionList';
import AddDistribution from './pages/AddDistribution';
import CatalogList from './pages/CatalogList';
import AddCatalog from './pages/AddCatalog';
import EditCatalog from './pages/EditCatalog';
import EditDistribution from './pages/EditDistribution';
import Sparql from './pages/Sparql';
import Quality from './pages/Quality';
import Profile from './pages/Profile';
import { Language, LanguageOptionsContext, sendPost, supportedLanguages, TokenContext, TokenResult, UserInfo, sendPost, supportedLanguages, useUserInfo } from './client';
import React, { useEffect, useState, useContext, useCallback } from 'react';
import PublisherList from './pages/PublisherList';
import UserList from './pages/UserList';
import EditUser from './pages/EditUser';
import AddUser from './pages/AddUser';
import Codelists from './pages/Codelists';
import AddPublisher from './pages/AddPublisher';
import InfoPageInvalidDelegation from './pages/InfoPageInvalidDelegation';
import InfoPageWaitingForApprove from './pages/InfoPageWaitingForApprove';
import {AxiosResponse, RawAxiosRequestHeaders} from 'axios';
import NotFound from './pages/NotFound';
import Invitation from './pages/Invitation';
import LoginInProgress from './pages/LoginInProgress';
import EditPublisher from './pages/EditPublisher';
import PublisherRegistration from './pages/PublisherRegistration';
import ChangeLicenses from './pages/ChangeLicenses';
import ODCommunityStartPage from "./pages/cms/ODCommunityStartPage";
import UserPage from "./pages/cms/UserPage";
import RegisterUser from "./pages/cms/RegisterUser";
import Login from "./pages/cms/Login";

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
                    } else if (userInfo.role === 'Superadmin') {
                        navigate('/');
                    } else if (userInfo.role !== 'Superadmin') {
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
    const [cmsUser, setCmsUser] = useState<CmsUser | null>(null);
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
                if (process.env.REACT_APP_NO_AUTH) {
                    setCmsUser({
                            id: 'id',
                            userName: 'ODKomunity tester',
                            normalizedUserName: undefined,
                            email: undefined,
                            normalizedEmail: undefined,
                            emailConfirmed: false,
                            passwordHash: undefined,
                            securityStamp: undefined,
                            phoneNumber: undefined,
                            phoneNumberConfirmed: false,
                            twoFactorEnabled: false,
                            lockoutEnd: undefined,
                            lockoutEnabled: false,
                            accessFailedCount: 0,
                            concurrencyStamp: undefined
                        });
                    
                    setUserInfo({
                        publisher: 'test',
                        publisherEmail: 'test@test.tst',
                        publisherHomePage: '',
                        publisherView: {
                            id: '',
                            key: '',
                            isPublic: true,
                            name: 'test',
                            datasetCount: 0,
                            themes: null
                        },
                        publisherActive: true,
                        publisherPhone: '',
                        id: '',
                        firstName: 'test',
                        lastName: 'test',
                        email: 'test@test.tst',
                        role: 'Superadmin',
                        companyName: 'test'
                    });
                } else {
                    setCmsUser(await getCmsUser());
                    
                    if (!headers['Authorization']) {
                        setUserInfo(null);
                        setUserInfoIsLoading(false);
                        return;
                    }

                    setUserInfoIsLoading(true);

                    setUserInfo((await sendPost('user-info', {}, headers)).data);
                }
                
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
            <CmsUserContext.Provider
                value={{
                    cmsUser: cmsUser,
                    setCmsUser: setCmsUser
                }}>
                <LanguageOptionsContext.Provider
                    value={{
                        language: language,
                        setLanguage: setLanguage
                    }}
                >
                    <BrowserRouter>
                        <Header />
                        <AppNavigator {...props} />
                        <div className="govuk-width-container">
                            <div className="govuk-grid-row">
                                <div className="govuk-grid-column-full">
                                    <Routes>
                                        <Route path="/" Component={HomePage} />
    
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
    
                                        {userInfo?.role === 'Superadmin' ? (
                                            <>
                                                <Route path="/sprava/poskytovatelia" Component={PublisherList} />
                                            <Route path="/sprava/poskytovatelia/pridat" Component={AddPublisher} />
                                            <Route path="/sprava/poskytovatelia/upravit/:id" Component={EditPublisher} />
                                                <Route path="/sprava/ciselniky" Component={Codelists} />
                                            </>
                                        ) : null}
                                        
                                        <Route path="/odkomunita" Component={ODCommunityStartPage} />
                                        <Route path="/odkomunita/register-user" Component={RegisterUser} />
                                        <Route path="/odkomunita/user-page" Component={UserPage} />
                                        <Route path="/odkomunita/login" Component={Login} />
    
                                        <Route path="*" Component={NotFound} />
                                    </Routes>
                                </div>
                            </div>
                        </div>
                        <Footer />
                    </BrowserRouter>
                </LanguageOptionsContext.Provider>
            </CmsUserContext.Provider>
        </TokenContext.Provider>
    );
}

export default App;
