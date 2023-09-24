import { Dataset, DatasetInput, useDatasetEdit, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DatasetForm } from "../components/DatasetForm";
import Loading from "../components/Loading";
import { useNavigate } from "react-router";

const transformEntityForEdit = (entity: Dataset): DatasetInput => {
    return {
        id: entity.id,
        isPublic: entity.isPublic,
        name: {'sk': entity.name ?? ''},
        description: {'sk': entity.description ?? ''},
        themes: entity.themes,
        accrualPeriodicity: entity.accrualPeriodicity,
        keywords: {'sk': entity.keywords ?? []},
        type: entity.type,
        spatial: entity.spatial,
        startDate: entity.temporal?.startDate ?? null,
        endDate: entity.temporal?.endDate ?? null,
        contactName: {'sk': entity.contactPoint?.name ?? ''},
        contactEmail: entity.contactPoint?.email ?? '',
        documentation: entity.documentation,
        specification: entity.specification,
        euroVocThemes: entity.euroVocThemes,
        spatialResolutionInMeters: entity.spatialResolutionInMeters,
        temporalResolution: entity.temporalResolution,
        isPartOf: entity.isPartOf
    }
};

export default function EditDataset()
{
    const [inputs, dataset, loading, setDataset, errors, saving, saveResult, save] = useDatasetEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();

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
                        <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/datasety');
                        }
                    }} disabled={saving}>
                            Uložiť dataset
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}