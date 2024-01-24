import PageHeader from '../components/PageHeader';
import Button from '../components/Button';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import { sendPut, useUserInfo, SaveResult, useDefaultHeaders, useDocumentTitle, TokenContext, PublisherInput } from '../client';
import { useContext, useEffect, useState } from 'react';
import { AxiosResponse } from 'axios';
import ValidationSummary from '../components/ValidationSummary';
import { useTranslation } from 'react-i18next';
import { ProfileFormControls } from '../components/ProfileFormControls';

export default function Profile() {
    const [profile, setProfile] = useState<PublisherInput | null>(null);
    const [saving, setSaving] = useState<boolean>();
    const [saveResult, setSaveResult] = useState<SaveResult | null>(null);
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('publisherProfile'));
    const tokenContext = useContext(TokenContext);

    const errors = saveResult?.errors ?? {};

    useEffect(() => {
        if (userInfo?.publisherView) {
            setProfile({
                website: userInfo.publisherHomePage ?? '',
                email: userInfo.publisherEmail ?? '',
                phone: userInfo.publisherPhone ?? '',
                legalForm: userInfo.publisherLegalForm ?? ''
            });
        }
    }, [userInfo]);

    async function save() {
        setSaving(true);
        try {
            const response: AxiosResponse<SaveResult> = await sendPut('profile', profile, headers);
            setSaveResult(response.data);
            if (tokenContext?.token) {
                tokenContext?.setToken({ ...tokenContext.token });
            }
        } finally {
            setSaving(false);
        }
    }

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publisherProfile') }]} />
            <MainContent>
                <PageHeader>{t('publisherProfile')}</PageHeader>
                {profile ? (
                    <>
                        {Object.keys(errors).length > 0 ? (
                            <ValidationSummary
                                elements={Object.entries(errors).map((k) => ({
                                    elementId: k[0],
                                    message: k[1]
                                }))}
                            />
                        ) : null}

                        <ProfileFormControls
                            publisher={profile}
                            setPublisher={(p) => setProfile({ ...profile, ...p })}
                            errors={errors}
                            saving={saving ?? false}
                        />

                        <Button style={{ marginRight: '20px' }} onClick={save} disabled={saving}>
                            {t('save')}
                        </Button>
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
