import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import { SaveResult, sendGet, sendPost, useDefaultHeaders, useDocumentTitle } from '../client';
import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { AxiosResponse } from 'axios';
import ValidationSummary from '../components/ValidationSummary';
import Button from '../components/Button';
import Checkbox from '../components/Checkbox';

type NotificationSetting = {
    email: string;
    isDisabled: boolean;
};

const auth = window.location.search;

export default function NotificationSettings() {
    const { t } = useTranslation();
    useDocumentTitle(t('notificationSettings'));

    const [setting, setSetting] = useState<NotificationSetting | null>(null);
    const [saving, setSaving] = useState<boolean>();
    const [saveResult, setSaveResult] = useState<SaveResult | null>(null);
    const headers = useDefaultHeaders();
    useDocumentTitle(t('publisherProfile'));

    const errors = saveResult?.errors ?? {};

    useEffect(() => {
        async function load() {
            if (auth || Object.values(headers).length > 0) {
                const setting = await sendGet('notification-setting' + auth, headers);
                setSetting({ isDisabled: false, ...setting.data });
            }
        }

        load();
    }, [headers]);

    async function save() {
        setSaving(true);
        try {
            const response: AxiosResponse<SaveResult> = await sendPost('notification-setting' + auth, setting, headers);
            setSaveResult(response.data);
        } finally {
            setSaving(false);
        }
    }

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('notificationSettings') }]} />
            <MainContent>
                <PageHeader>{t('notificationSettings')}</PageHeader>

                {setting ? (
                    <>
                        {Object.keys(errors).length > 0 ? (
                            <ValidationSummary
                                elements={Object.entries(errors).map((k) => ({
                                    elementId: k[0],
                                    message: k[1]
                                }))}
                            />
                        ) : null}

                        <p className="govuk-body">Nastavenia pre e-mailovú adresu: {setting.email}</p>

                        <div style={{ marginBottom: '2em' }}>
                            <Checkbox
                                label={t('notificationsAreDisabled')}
                                checked={setting.isDisabled}
                                onCheckedChange={(v) => setSetting({ ...setting, isDisabled: v })}
                            />
                        </div>

                        <Button style={{ marginRight: '20px' }} onClick={save} disabled={saving}>
                            {t('save')}
                        </Button>
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
