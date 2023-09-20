import { Dataset, DatasetInput, useDatasetAdd, useDatasetEdit, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DatasetForm } from "../components/DatasetForm";
import Loading from "../components/Loading";

const transformEntityForEdit = (entity: Dataset): DatasetInput => {
    return {
        isPublic: true,
        name: {'sk': ''},
        description: {'sk': ''},
        themes: [],
        accrualPeriodicity: 'http://publications.europa.eu/resource/authority/frequency/IRREG',
        keywords: {'sk': []},
        type: [],
        spatial: [],
        startDate: null,
        endDate: null,
        contactName: {},
        contactEmail: null,
        documentation: null,
        specification: null,
        euroVocThemes: [],
        spatialResolutionInMeters: null,
        temporalResolution: null,
        isPartOf: null
    }
};

export default function EditDataset()
{
    const [inputs, dataset, loading, setDataset, errors, saving, saveResult, save] = useDatasetEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam datasetov', link: '/sprava/datasety'}, {title: 'Upraviť dataset'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Upraviť dataset</PageHeader>
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
                        <DatasetForm dataset={inputs} setDataset={setDataset} errors={errors} userInfo={userInfo} />
                        <Button style={{marginRight: '20px'}} onClick={save} disabled={saving}>
                            Uložiť dataset
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}