import { useDataset, useDistributionAdd, useDistributions, useDocumentTitle, useUserInfo } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { DistributionForm } from '../components/DistributionForm';
import { useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useEffect, useState } from 'react';

export default function AddDistribution() {
    const { datasetId } = useParams();
    const [dataset] = useDataset(datasetId);

    const [distribution, setDistribution, errors, saving, save] = useDistributionAdd({
        datasetId: datasetId ?? '',
        authorsWorkType: 'http://publications.europa.eu/resource/authority/licence/CC_BY_4_0',
        originalDatabaseType: 'http://publications.europa.eu/resource/authority/licence/CC_BY_4_0',
        databaseProtectedBySpecialRightsType: 'http://publications.europa.eu/resource/authority/licence/CC_BY_4_0',
        personalDataContainmentType: 'https://data.gov.sk/def/personal-data-occurence-type/2',
        authorName: null,
        originalDatabaseAuthorName: null,
        downloadUrl: null,
        format: 'http://publications.europa.eu/resource/authority/file-type/CSV',
        mediaType: 'http://www.iana.org/assignments/media-types/text/csv',
        compressFormat: null,
        packageFormat: null,
        conformsTo: null,
        title: null,
        fileId: null,
        endpointUrl: null,
        documentation: null,
        applicableLegislations: [],
        isDataService: false,
        endpointDescription: null,
        hvdCategory: null,
        contactName: {},
        contactEmail: null
    });

    const [distributions, , , loadingDistributions, errorDistributions] = useDistributions(
        datasetId ? { filters: { parent: [datasetId] }, page: 1, pageSize: 1 } : { page: 0 }
    );

    const [licensesApplied, setLicensesApplied] = useState(false);
    useEffect(() => {
        if (distributions && distributions.items.length > 0 && !licensesApplied) {
            setLicensesApplied(true);
            const previous = distributions.items[0];
            setDistribution({
                authorsWorkType: previous.termsOfUse?.authorsWorkType ?? null,
                originalDatabaseType: previous.termsOfUse?.originalDatabaseType ?? null,
                databaseProtectedBySpecialRightsType: previous.termsOfUse?.databaseProtectedBySpecialRightsType ?? null,
                personalDataContainmentType: previous.termsOfUse?.personalDataContainmentType ?? null,
                authorName: previous.termsOfUse?.authorName ?? null,
                originalDatabaseAuthorName: previous.termsOfUse?.originalDatabaseAuthorName ?? null
            });
        }
    }, [distributions, setDistribution, licensesApplied]);

    const loading = loadingDistributions;

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('newDistribution'));

    return (
        <>
            <Breadcrumbs
                items={[
                    { title: t('nkod'), link: '/' },
                    { title: t('distributionList'), link: '/sprava/distribucie/' + datasetId },
                    { title: t('newDistribution') }
                ]}
            />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('newDistribution')}</PageHeader>
                    {userInfo?.publisherView ? (
                        <p className="govuk-body nkod-publisher-name">
                            <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                            <br />
                            {userInfo.publisherView.name}
                        </p>
                    ) : null}

                    {loading ? <Loading /> : null}
                    {errorDistributions ? <ErrorAlert error={errorDistributions} /> : null}

                    {Object.keys(errors).length > 0 ? (
                        <ValidationSummary
                            elements={Object.entries(errors).map((k) => ({
                                elementId: k[0],
                                message: k[1]
                            }))}
                        />
                    ) : null}

                    {!loading ? (
                        <>
                            <DistributionForm distribution={distribution} setDistribution={setDistribution} errors={errors} saving={saving} dataset={dataset} />

                            <Button
                                style={{ marginRight: '20px' }}
                                onClick={async () => {
                                    const result = await save();
                                    if (result?.success) {
                                        navigate('/sprava/distribucie/' + datasetId);
                                    }
                                }}
                                disabled={saving}
                            >
                                {t('save')}
                            </Button>
                        </>
                    ) : null}
                </div>
            </MainContent>
        </>
    );
}
