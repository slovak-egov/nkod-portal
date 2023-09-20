import { Dataset, DatasetInput, LocalCatalog, LocalCatalogInput, useDatasetAdd, useDatasetEdit, useLocalCatalogEdit, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DatasetForm } from "../components/DatasetForm";
import Loading from "../components/Loading";
import { LocalCatalogForm } from "../components/LocalCatalogForm";
import { useNavigate } from "react-router";

const transformEntityForEdit = (entity: LocalCatalog): LocalCatalogInput => {
    return {
        id: entity.id,
        isPublic: entity.isPublic,
        name: {'sk': entity.name ?? ''},
        description: {'sk': entity.description ?? ''},
        contactName: {'sk': entity.contactPoint?.name ?? ''},
        contactEmail: entity.contactPoint?.email ?? '',
        homePage: entity.homePage,
    }
};

export default function EditCatalog()
{
    const [inputs, catalog, loading, setCatalog, errors, saving, saveResult, save] = useLocalCatalogEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam lokálnych katalógov', link: '/sprava/lokalne-katalógy'}, {title: 'Upraviť katalóg'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Upraviť katalóg</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    {
                        !loading && inputs ? <>
                        <LocalCatalogForm catalog={inputs} setCatalog={setCatalog} errors={errors} userInfo={userInfo} />
                        <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/lokalne-katalogy');
                        }
                    }} disabled={saving}>
                            Uložiť katalóg
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}