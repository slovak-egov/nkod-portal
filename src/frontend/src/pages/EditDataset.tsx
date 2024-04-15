import { Dataset, DatasetInput, useDatasetEdit, useDocumentTitle, useUserInfo } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { DatasetForm } from '../components/DatasetForm';
import Loading from '../components/Loading';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';

const transformEntityForEdit = (entity: Dataset): DatasetInput => {
    return {
        id: entity.id,
        isPublic: entity.isPublic,
        name: entity.nameAll ?? {},
        description: entity.descriptionAll ?? {},
        themes: entity.themes,
        accrualPeriodicity: entity.accrualPeriodicity,
        keywords: entity.keywordsAll ?? {},
        type: entity.type,
        spatial: entity.spatial,
        startDate: entity.temporal?.startDate ?? null,
        endDate: entity.temporal?.endDate ?? null,
        contactName: entity?.contactPoint?.nameAll ?? {},
        contactEmail: entity.contactPoint?.email ?? '',
        landingPage: entity.landingPage,
        specification: entity.specification,
        euroVocThemes: entity.euroVocThemes,
        spatialResolutionInMeters: entity.spatialResolutionInMeters?.toLocaleString() ?? null,
        temporalResolution: entity.temporalResolution,
        isPartOf: entity.isPartOf,
        isSerie: entity.isSerie
    };
};

export default function EditDataset() {
    const [inputs, , loading, setDataset, errors, saving, save] = useDatasetEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('editDataset'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('datasetList'), link: '/sprava/datasety' }, { title: t('editDataset') }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('editDataset')}</PageHeader>
                    {userInfo?.publisherView ? (
                        <p className="govuk-body nkod-publisher-name">
                            <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                            <br />
                            {userInfo.publisherView.name}
                        </p>
                    ) : null}

                    {Object.keys(errors).length > 0 ? (
                        <ValidationSummary
                            elements={Object.entries(errors).map((k) => ({
                                elementId: k[0],
                                message: k[1]
                            }))}
                        />
                    ) : null}

                    {!loading && inputs ? (
                        <>
                            <DatasetForm dataset={inputs} setDataset={setDataset} errors={errors} userInfo={userInfo} saving={saving} />
                            <Button
                                style={{ marginRight: '20px' }}
                                onClick={async () => {
                                    const result = await save();
                                    if (result?.success) {
                                        navigate('/sprava/datasety');
                                    }
                                }}
                                disabled={saving}
                            >
                                {t('save')}
                            </Button>
                        </>
                    ) : (
                        <Loading />
                    )}
                </div>
            </MainContent>
        </>
    );
}
