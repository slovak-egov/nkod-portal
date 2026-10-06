import { AxiosError, AxiosResponse } from 'axios';
import { useCallback, useEffect, useRef, useState } from 'react';
import { sendGet, sendPut, useDefaultHeaders, useDocumentTitle } from '../client';
import Alert from '../components/Alert';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import Loading from '../components/Loading';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import TextArea from '../components/TextArea';

type AppSettings = {
    allowedIframeHosts: string;
};

export default function AppSettingsPage() {
    const [allowedIframeHosts, setAllowedIframeHosts] = useState('');
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [loadError, setLoadError] = useState<string | null>(null);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [saveSuccess, setSaveSuccess] = useState(false);
    const headers = useDefaultHeaders();
    const initialLoadDone = useRef(false);

    const pageTitle = 'Nastavenia aplikácie';
    useDocumentTitle(pageTitle);

    useEffect(() => {
        if (initialLoadDone.current) return;
        if (!headers['Authorization']) return;

        async function load() {
            setLoading(true);
            setLoadError(null);
            try {
                const response: AxiosResponse<AppSettings> = await sendGet('app-settings', headers);
                setAllowedIframeHosts(response.data?.allowedIframeHosts ?? '');
                initialLoadDone.current = true;
            } catch (err) {
                if (err instanceof Error) {
                    setLoadError(err.message);
                }
            } finally {
                setLoading(false);
            }
        }

        load();
    }, [headers]);

    const save = useCallback(async () => {
        setSaving(true);
        setSaveError(null);
        setSaveSuccess(false);
        try {
            await sendPut<AppSettings>('app-settings', { allowedIframeHosts }, headers);
            setSaveSuccess(true);
        } catch (err) {
            if (err instanceof AxiosError) {
                setSaveError(err.response?.data ?? err.message);
            } else if (err instanceof Error) {
                setSaveError(err.message);
            }
        } finally {
            setSaving(false);
        }
    }, [allowedIframeHosts, headers]);

    return (
        <>
            <Breadcrumbs items={[{ title: 'NKOD', link: '/' }, { title: pageTitle }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{pageTitle}</PageHeader>

                    {loadError ? <Alert type="warning">{loadError}</Alert> : null}
                    {saveError ? <Alert type="warning">{saveError}</Alert> : null}
                    {saveSuccess ? <Alert type="info">Nastavenia boli uložené.</Alert> : null}

                    {loading ? (
                        <Loading />
                    ) : (
                        <>
                            <FormElementGroup
                                label="Povolené hosty pre iframe"
                                hint="Každý host na samostatnom riadku. Podporované sú hviezdičky (napr. *.example.com). Zadajte * pre povolenie všetkých hostov."
                                element={(id) => (
                                    <TextArea
                                        id={id}
                                        rows={10}
                                        value={allowedIframeHosts}
                                        disabled={saving}
                                        onChange={(e) => {
                                            setSaveSuccess(false);
                                            setAllowedIframeHosts(e.target.value);
                                        }}
                                    />
                                )}
                            />

                            <Button onClick={save} disabled={saving}>
                                Uložiť
                            </Button>
                        </>
                    )}
                </div>
            </MainContent>
        </>
    );
}
