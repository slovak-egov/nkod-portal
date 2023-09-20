import Header from './components/Header';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
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

function App() {
    return <BrowserRouter>
      <Alert type='warning'>
        Vývojová verzia Národného katalógu otvorených dát (20230920)
      </Alert>
      <Header />
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
                <Route path="/sprava/distribucie/:datasetId/pridat" Component={AddDistribution} />
                <Route path="/sprava/distribucie/:datasetId/upravit/:id" Component={EditDistribution} />

                <Route path="/sprava/lokalne-katalogy" Component={CatalogList} />
                <Route path="/sprava/lokalne-katalogy/pridat" Component={AddCatalog} />
                <Route path="/sprava/lokalne-katalogy/upravit/:id" Component={EditCatalog} />
                
                
                {/* <Route path="/kvalita-metadat" Component={Quality} />
                <Route path="/sparql" Component={Sparql} />
                <Route path="/dataset" Component={DetailDataset} />
                <Route path="/katalog" Component={DetailCatalog} />
                <Route path="/admin/distributions" Component={DistributionList} />
                <Route path="/admin/distributions/add" Component={AddDistribution} />
                <Route path="/admin/catalogs" Component={CatalogList} />
                <Route path="/admin/catalogs/add" Component={AddCatalog} />
                <Route path="/admin/catalogs/error" Component={AddCatalogError} />
                <Route path="/admin/users" Component={UserList} />
                <Route path="/admin/users/add" Component={AddUser} />
                <Route path="/register" Component={AddPublisher} />
                <Route path="/admin/publishers" Component={PublisherList} />
                <Route path="/admin/codebooks" Component={CodebookList} />
                <Route path="/admin/profile" Component={PublisherProfile} /> */}
              </Routes>
               </div>
          </div>
      </div>
      <Footer />
    </BrowserRouter>;
}

export default App;
