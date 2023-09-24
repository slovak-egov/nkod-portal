import Header from './components/Header';
import { BrowserRouter, Route, Routes, useNavigate } from 'react-router-dom';
import { Footer } from './components/Footer';
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
import { TokenContext, TokenResult, useUserInfo } from './client';
import { useEffect } from 'react';
import PublisherList from './pages/PublisherList';
import UserList from './pages/UserList';
import EditUser from './pages/EditUser';
import AddUser from './pages/AddUser';
import Codelists from './pages/Codelists';
import AddPublisher from './pages/AddPublisher';

type Props = {
  extenalToken: TokenResult|null;
}

function AppNavigator(props : Props) {
  const token = props.extenalToken;
  const navigate = useNavigate();
  const [userInfo] = useUserInfo();

  useEffect(() => {
    if (token?.redirectUrl) {
      navigate(token.redirectUrl);
    } else if (userInfo !== null) {
      if (userInfo.publisherView === null) {
        if (userInfo.role === 'PublisherAdmin') {
          navigate('/registracia');
        } else {
          navigate('/sprava/neplatne-zastupenie');
        }
      } else if (!userInfo.publisherActive) {
        navigate('/sprava/caka-na-schvalenie');
      } else if (userInfo.role === null) {
        navigate('/sprava/bez-opravnenia');
      } else {
        navigate('/');
      }
    }
  }, [token, userInfo]);

  return null;
}

function App(props : Props) {
    return <TokenContext.Provider value={props.extenalToken}><BrowserRouter>
      <Alert type='warning' style={{margin: 0}}>
        Vývojová verzia Národného katalógu otvorených dát (20230924)
      </Alert>
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

                <Route path="/sprava/datasety" Component={DatasetList} />
                <Route path="/sprava/datasety/pridat" Component={AddDataset} />
                <Route path="/sprava/datasety/upravit/:id" Component={EditDataset} />
                
                <Route path="/sprava/distribucie/:datasetId" Component={DistributionList} />
                <Route path="/sprava/distribucie/:datasetId/pridat" Component={AddDistribution} />
                <Route path="/sprava/distribucie/:datasetId/upravit/:id" Component={EditDistribution} />

                <Route path="/sprava/lokalne-katalogy" Component={CatalogList} />
                <Route path="/sprava/lokalne-katalogy/pridat" Component={AddCatalog} />
                <Route path="/sprava/lokalne-katalogy/upravit/:id" Component={EditCatalog} />

                <Route path="/sprava/poskytovatelia" Component={PublisherList} />
                <Route path="/sprava/cisleniky" Component={Codelists} />
                
                <Route path="/sprava/pouzivatelia" Component={UserList} />
                <Route path="/sprava/pouzivatelia/pridat" Component={AddUser} />
                <Route path="/sprava/pouzivatelia/upravit/:id" Component={EditUser} />
                <Route path="/sprava/profil" Component={Profile} />

                <Route path="/sparql" Component={Sparql} />
                <Route path="/kvalita-metadat" Component={Quality} />
                <Route path="/registracia" Component={AddPublisher} />
              </Routes>
               </div>
          </div>
      </div>
      <Footer />
    </BrowserRouter></TokenContext.Provider>;
}

export default App;
