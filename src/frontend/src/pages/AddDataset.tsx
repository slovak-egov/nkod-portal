import { useDatasetAdd, useDocumentTitle, useUserInfo } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { DatasetForm } from '../components/DatasetForm';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import AlertPublisher2 from '../components/AlertPublisher2';

export default function AddDataset() {
    const [dataset, setDataset, errors, saving, save] = useDatasetAdd({
        isPublic: true,
        name: { sk: '' },
        description: { sk: '' },
        themes: [],
        accrualPeriodicity: 'http://publications.europa.eu/resource/authority/frequency/IRREG',
        keywords: { sk: [] },
        type: [],
        spatial: [],
        startDate: null,
        endDate: null,
        contactName: {},
        contactEmail: null,
        landingPage: null,
        specification: null,
        euroVocThemes: [],
        spatialResolutionInMeters: null,
        temporalResolution: null,
        isPartOf: null,
        isSerie: false
    });

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('newDataset'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('datasetList'), link: '/sprava/datasety' }, { title: 'Nový dataset' }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('newDataset')}</PageHeader>
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

                    <DatasetForm dataset={dataset} setDataset={setDataset} errors={errors} userInfo={userInfo} saving={saving} />

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
                        {t('saveDataset')}
                    </Button>

                    {!dataset.isSerie ? (
                        <Button
                            disabled={saving}
                            onClick={async () => {
                                const result = await save();
                                if (result?.success) {
                                    navigate('/sprava/distribucie/' + result?.id + '/pridat');
                                }
                            }}
                        >
                            {t('saveAndAddDistribution')}
                        </Button>
                    ) : null}
                </div>
            </MainContent>
        </>
    );
}
