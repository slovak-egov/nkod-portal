import { useDatasetAdd, useLocalCatalogAdd, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DatasetForm } from "../components/DatasetForm";
import { LocalCatalogForm } from "../components/LocalCatalogForm";
import { useNavigate } from "react-router";


export default function AddCatalog()
{
    const [catalog, setCatalog, errors, saving, saveResult, save] = useLocalCatalogAdd({
        isPublic: true,
        name: {'sk': ''},
        description: {'sk': ''},
        contactName: {},
        contactEmail: null,
        homePage: null,
    });

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam lokálnych katalógov', link: '/sprava/lokalne-katalogy'}, {title: 'Nový katalóg'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Nový katalóg</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <LocalCatalogForm catalog={catalog} setCatalog={setCatalog} errors={errors} userInfo={userInfo} />

                    <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/lokalne-katalogy');
                        }
                    }} disabled={saving}>
                        Uložiť katalóg
                    </Button>
                </div>
            </MainContent>
        </>;
}