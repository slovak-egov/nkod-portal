import { useDocumentTitle, useLocalCatalogAdd, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { LocalCatalogForm } from "../components/LocalCatalogForm";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";


export default function AddCatalog()
{
    const [catalog, setCatalog, errors, saving, save] = useLocalCatalogAdd({
        isPublic: true,
        name: {'sk': ''},
        description: {'sk': ''},
        contactName: {},
        contactEmail: null,
        homePage: null,
        type: 'https://data.gov.sk/def/local-catalog-type/1',
        endpointUrl: null,
    });

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('newCatalog'));

    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('localCatalogList'), link: '/sprava/lokalne-katalogy'}, {title: t('newCatalog')}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('newCatalog')}</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <LocalCatalogForm catalog={catalog} 
                                      setCatalog={setCatalog} 
                                      errors={errors}
                                      saving={saving} />

                    <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/lokalne-katalogy');
                        }
                    }} disabled={saving}>
                        {t('save')}
                    </Button>
                </div>
            </MainContent>
        </>;
}