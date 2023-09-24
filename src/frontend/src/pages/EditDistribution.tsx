import { Distribution, DistributionInput, useDataset, useDistributionEdit, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import Loading from "../components/Loading";
import { useNavigate, useParams } from "react-router";
import { DistributionForm } from "../components/DistributionForm";

const transformEntityForEdit = (entity: Distribution): DistributionInput => {
    return {
        id: entity.id,
        datasetId: entity.datasetId,
        authorsWorkType: entity.termsOfUse?.authorsWorkType ?? null,
        originalDatabaseType: entity.termsOfUse?.originalDatabaseType ?? null,
        databaseProtectedBySpecialRightsType: entity.termsOfUse?.databaseProtectedBySpecialRightsType ?? null,
        personalDataContainmentType: entity.termsOfUse?.personalDataContainmentType ?? null,
        downloadUrl: entity.downloadUrl ?? null,
        accessUrl: entity.accessUrl ?? null,
        format: entity.format ?? null,
        mediaType: entity.mediaType ?? null,
        compressFormat: entity.compressFormat ?? null,
        packageFormat: entity.packageFormat ?? null,
        conformsTo: entity.conformsTo ?? null,
        title: entity.title ? {'sk': entity.title} : null,
        fileId: null
    }
};

export default function EditDistribution()
{
    const { datasetId } = useParams();
    const [dataset] = useDataset(datasetId);
    const [inputs, distribution, loading, setDistribution, errors, saving, saveResult, save] = useDistributionEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam distribúcií', link: '/sprava/distribucie/' + datasetId}, {title: 'Upraviť distribúciu'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Upraviť distribúciu</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}

                    </p> : null}

                    {dataset ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Dataset</span><br />
                    {dataset.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    {
                        !loading && inputs ? <>
                        <DistributionForm distribution={inputs} setDistribution={setDistribution} errors={errors} userInfo={userInfo} />
                        <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/distribucie/' + datasetId);
                        }
                    }} disabled={saving}>
                            Uložiť distribúciu
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}