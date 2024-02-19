import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import {
    CodelistValue,
    Distribution,
    DistributionInput,
    RequestQuery,
    Response,
    knownCodelists,
    sendPost,
    sendPut,
    useCodelists,
    useDefaultHeaders,
    useDocumentTitle,
    useUserInfo
} from '../client';
import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useTranslation } from 'react-i18next';
import AlertPublisher from '../components/AlertPublisher';
import FormElementGroup from '../components/FormElementGroup';
import SelectElementItems from '../components/SelectElementItems';
import { useState } from 'react';
import Button from '../components/Button';
import { AxiosResponse } from 'axios';
import { useNavigate } from 'react-router';

const requiredCodelists = [knownCodelists.distribution.license, knownCodelists.distribution.personalDataContainmentType];

type LicenseSettings = {
    authorsWorkType: string;
    originalDatabaseType: string;
    databaseProtectedBySpecialRightsType: string;
    personalDataContainmentType: string;
};

export default function ChangeLicenses() {
    const [licenses, setLicenses] = useState<LicenseSettings>({
        authorsWorkType: '',
        originalDatabaseType: '',
        databaseProtectedBySpecialRightsType: '',
        personalDataContainmentType: ''
    });
    const [saving, setSaving] = useState(false);
    const [changed, setChanged] = useState(0);
    const [total, setTotal] = useState(0);

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const [userInfo] = useUserInfo();
    const { t } = useTranslation();
    useDocumentTitle(t('changeLicenses'));
    const navigate = useNavigate();
    const headers = useDefaultHeaders();

    const loading = loadingCodelists;
    const error = errorCodelists;

    const licenseCodelist = codelists.find((c) => c.id === knownCodelists.distribution.license);
    const personalDataContainmentTypeCodelist = codelists.find((c) => c.id === knownCodelists.distribution.personalDataContainmentType);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('changeLicenses') }]} />
            <MainContent>
                <AlertPublisher />
                <PageHeader>{t('changeLicenses')}</PageHeader>
                {userInfo?.publisherView ? (
                    <p className="govuk-body nkod-publisher-name">
                        <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                        <br />
                        {userInfo.publisherView.name}
                    </p>
                ) : null}

                {loading ? <Loading /> : null}
                {error ? <ErrorAlert error={error} /> : null}

                {licenseCodelist ? (
                    <FormElementGroup
                        label={t('authorWorkType')}
                        element={(id) => (
                            <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={[{ id: '', label: t('leaveAsIs') }, ...licenseCodelist.values]}
                                selectedValue={licenses.authorsWorkType ?? ''}
                                renderOption={(v) => v.label}
                                getValue={(v) => v.id}
                                onChange={(v) => {
                                    setLicenses((prev) => ({ ...prev, authorsWorkType: v }));
                                }}
                            />
                        )}
                    />
                ) : null}

                {licenseCodelist ? (
                    <FormElementGroup
                        label={t('originalDatabaseType')}
                        element={(id) => (
                            <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={[{ id: '', label: t('leaveAsIs') }, ...licenseCodelist.values]}
                                selectedValue={licenses.originalDatabaseType ?? ''}
                                renderOption={(v) => v.label}
                                getValue={(v) => v.id}
                                onChange={(v) => {
                                    setLicenses((prev) => ({ ...prev, originalDatabaseType: v }));
                                }}
                            />
                        )}
                    />
                ) : null}

                {licenseCodelist ? (
                    <FormElementGroup
                        label={t('specialDatabaseRights')}
                        element={(id) => (
                            <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={[{ id: '', label: t('leaveAsIs') }, ...licenseCodelist.values]}
                                selectedValue={licenses.databaseProtectedBySpecialRightsType ?? ''}
                                renderOption={(v) => v.label}
                                getValue={(v) => v.id}
                                onChange={(v) => {
                                    setLicenses((prev) => ({ ...prev, databaseProtectedBySpecialRightsType: v }));
                                }}
                            />
                        )}
                    />
                ) : null}

                {personalDataContainmentTypeCodelist ? (
                    <FormElementGroup
                        label={t('personalDataType')}
                        element={(id) => (
                            <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={[{ id: '', label: t('leaveAsIs') }, ...personalDataContainmentTypeCodelist.values]}
                                selectedValue={licenses.personalDataContainmentType ?? ''}
                                renderOption={(v) => v.label}
                                getValue={(v) => v.id}
                                onChange={(v) => {
                                    setLicenses((prev) => ({ ...prev, personalDataContainmentType: v }));
                                }}
                            />
                        )}
                    />
                ) : null}

                {total > 0 ? (
                    <p className="govuk-body">
                        Prebieha zmena licencií, dokončených {changed} z {total}.
                    </p>
                ) : null}

                <Button
                    style={{ marginRight: '20px' }}
                    onClick={async () => {
                        setSaving(true);
                        const publisher = userInfo?.publisher;
                        if (publisher) {
                            try {
                                setChanged(0);
                                setTotal(0);

                                const response: AxiosResponse<Response<Distribution>> = await sendPost<RequestQuery>(
                                    'distributions/search',
                                    {
                                        pageSize: -1,
                                        page: 1,
                                        language: 'sk',
                                        filters: {
                                            publishers: [publisher]
                                        },
                                        requiredFacets: []
                                    },
                                    headers
                                );

                                setTotal(response.data.totalCount);

                                for (const distribution of response.data.items) {
                                    const input: Partial<DistributionInput> = { id: distribution.id };
                                    if (licenses.authorsWorkType) {
                                        input.authorsWorkType = licenses.authorsWorkType;
                                    }
                                    if (licenses.originalDatabaseType) {
                                        input.originalDatabaseType = licenses.originalDatabaseType;
                                    }
                                    if (licenses.databaseProtectedBySpecialRightsType) {
                                        input.databaseProtectedBySpecialRightsType = licenses.databaseProtectedBySpecialRightsType;
                                    }
                                    if (licenses.personalDataContainmentType) {
                                        input.personalDataContainmentType = licenses.personalDataContainmentType;
                                    }

                                    await sendPut<Partial<DistributionInput>>('distributions/licences', input, headers);
                                    setChanged((c) => c + 1);
                                }

                                navigate('/sprava/datasety');
                            } catch {
                            } finally {
                                setSaving(false);
                            }
                        }
                    }}
                    disabled={saving}
                >
                    {t('save')}
                </Button>
            </MainContent>
        </>
    );
}
