import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import { sendPut, useUserInfo, SaveResult, useDefaultHeaders, useDocumentTitle, TokenContext } from "../client";
import { useContext, useEffect, useState } from "react";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { AxiosResponse } from "axios";
import ValidationSummary from "../components/ValidationSummary";
import { useTranslation } from "react-i18next";

type Profile = {
    website: string;
    email: string;
    phone: string;
}

export default function Profile()
{
    const [profile, setProfile] = useState<Profile|null>(null);
    const [saving, setSaving] = useState<boolean>();
    const [saveResult, setSaveResult] = useState<SaveResult|null>(null);    
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const {t} = useTranslation();
    useDocumentTitle(t('publisherProfile'));
    const tokenContext = useContext(TokenContext);

    const errors = saveResult?.errors ?? {};

    useEffect(() => {
        if (userInfo?.publisherView) {
            setProfile({
                website: userInfo.publisherHomePage ?? '',
                email: userInfo.publisherEmail ?? '',
                phone: userInfo.publisherPhone ?? ''
            });
        }
    }, [userInfo]);

    async function save() {
        setSaving(true);
        try {
            const response: AxiosResponse<SaveResult> = await sendPut('profile', profile, headers);
            setSaveResult(response.data);
            if (tokenContext?.token) {
                tokenContext?.setToken({...tokenContext.token});
            }
        } finally {
            setSaving(false);
        }
    }

    return <>
    <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('publisherProfile')}]} />
            <MainContent>
                <PageHeader>{t('publisherProfile')}</PageHeader>
                {profile ? <>
                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <FormElementGroup label={t('websiteAddress')} errorMessage={errors['homePage']} element={id => <BaseInput id={id} disabled={saving} value={profile.website ?? ''} onChange={e => setProfile({...profile, website: e.target.value})} />} />
                    <FormElementGroup label={t('emailAddress')} errorMessage={errors['email']} element={id => <BaseInput id={id} disabled={saving} value={profile.email ?? ''} onChange={e => setProfile({...profile, email: e.target.value})} />} />
                    <FormElementGroup label={t('phoneContact')} errorMessage={errors['phone']} element={id => <BaseInput id={id} disabled={saving} value={profile.phone ?? ''} onChange={e => setProfile({...profile, phone: e.target.value})} />} />

                    <Button style={{marginRight: '20px'}} onClick={save} disabled={saving}>
                            {t('save')}
                        </Button>
                </> : null}
            </MainContent>
        </>;
}