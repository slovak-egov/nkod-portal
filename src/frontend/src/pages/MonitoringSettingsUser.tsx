import { MonitoringSettings, SaveResult, sendGet, sendPost, useDefaultHeaders, useDocumentTitle, useLocalCatalogs } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { useTranslation } from 'react-i18next';
import Loading from '../components/Loading';
import { useCallback, useEffect, useState } from 'react';
import { AxiosError, AxiosResponse } from 'axios';
import MonitoringNotificationSettingsControl from '../components/MonitoringNotificationSettingsControl';
import FormElementGroup from '../components/FormElementGroup';
import MultiSelectElementItems from '../components/MultiSelecteElementItems';

export default function MonitoringSettingsUser() {
    const [entity, setEntity] = useState<MonitoringSettings>({});
    const [errors, setErrors] = useState<{ [id: string]: string }>({});
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(false);
    const headers = useDefaultHeaders();

    const [catalogs, , , loadingCatalogs] = useLocalCatalogs({ pageSize: 10000, page: 1 });

    useEffect(() => {
        async function load() {
            setLoading(true);
            try {
                const response: AxiosResponse<MonitoringSettings> = await sendGet('monitoring-settings', headers);
                if (response.data) {
                    setEntity(response.data ?? {});
                }
            } catch (err) {
                if (err instanceof Error) {
                    setErrors({ load: err.message });
                }
            } finally {
                setLoading(false);
            }
        }

        load();
    }, []);

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<SaveResult> = await sendPost('monitoring-settings', entity, headers);
            setErrors(response.data.errors ?? {});
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? { generic: err.message });
            } else if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, headers]);

    const { t } = useTranslation();
    const pageTitle = t('Nastavenia notifikácie');
    useDocumentTitle(pageTitle);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: pageTitle }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{pageTitle}</PageHeader>

                    {Object.keys(errors).length > 0 ? (
                        <ValidationSummary
                            elements={Object.entries(errors).map((k) => ({
                                elementId: k[0],
                                message: k[1]
                            }))}
                        />
                    ) : null}

                    {!loading && !loadingCatalogs ? (
                        <>
                            <MonitoringNotificationSettingsControl errors={errors} settings={entity} setSettings={setEntity} disabled={saving} />

                            <div>
                                <FormElementGroup
                                    label="Ignorovať problémy pri katalógoch"
                                    errorMessage={errors['ignorelocalcatalogs']}
                                    element={(id) => (
                                        <select
                                            id={id}
                                            multiple
                                            value={entity.ignoreLocalCatalogs ?? []}
                                            disabled={saving}
                                            size={20}
                                            onChange={(e) =>
                                                setEntity({ ...entity, ignoreLocalCatalogs: Array.from(e.target.selectedOptions, (o) => o.value) })
                                            }
                                        >
                                            {(catalogs?.items ?? []).map((c) => (
                                                <option key={c.id} value={c.id}>
                                                    {c.name}
                                                </option>
                                            ))}
                                        </select>
                                    )}
                                />
                            </div>

                            <Button
                                style={{ marginRight: '20px' }}
                                onClick={async () => {
                                    const result = await save();
                                    if (result?.success) {
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
