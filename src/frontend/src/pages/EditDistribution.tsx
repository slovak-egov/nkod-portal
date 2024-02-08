import { Distribution, DistributionInput, useDataset, useDistributionEdit, useDocumentTitle, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import Loading from "../components/Loading";
import { useNavigate, useParams } from "react-router";
import { DistributionForm } from "../components/DistributionForm";
import { useTranslation } from "react-i18next";

export const transformEntityForEdit = (entity: Distribution): DistributionInput => {
    return {
        id: entity.id,
        datasetId: entity.datasetId,
        authorsWorkType: entity.termsOfUse?.authorsWorkType ?? null,
        originalDatabaseType: entity.termsOfUse?.originalDatabaseType ?? null,
        databaseProtectedBySpecialRightsType: entity.termsOfUse?.databaseProtectedBySpecialRightsType ?? null,
        personalDataContainmentType: entity.termsOfUse?.personalDataContainmentType ?? null,
        downloadUrl: entity.downloadUrl ?? null,
        format: entity.format ?? null,
        mediaType: entity.mediaType ?? null,
        compressFormat: entity.compressFormat ?? null,
        packageFormat: entity.packageFormat ?? null,
        conformsTo: entity.conformsTo ?? null,
        title: entity.titleAll ?? {},
        fileId: null
    }
};

export default function EditDistribution()
{
    const { datasetId } = useParams();
    const [dataset] = useDataset(datasetId);
    const [inputs, , loading, setDistribution, errors, saving, save] = useDistributionEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('editDistribution'));

    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('distributionList'), link: '/sprava/distribucie/' + datasetId}, {title: t('editDistribution')}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('editDistribution')}</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}

                    </p> : null}

                    {dataset ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('dataset')}</span><br />
                    {dataset.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    {
                        !loading && inputs ? <>
                        <DistributionForm distribution={inputs} 
                                          setDistribution={setDistribution} 
                                          errors={errors}
                                          saving={saving} />
                                          
                        <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/distribucie/' + datasetId);
                        }
                    }} disabled={saving}>
                            {t('save')}
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}