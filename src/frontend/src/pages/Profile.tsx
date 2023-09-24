import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Table from "../components/Table";
import TableHead from "../components/TableHead";
import TableRow from "../components/TableRow";
import TableHeaderCell from "../components/TableHeaderCell";
import TableBody from "../components/TableBody";
import TableCell from "../components/TableCell";
import Pagination from "../components/Pagination";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import { sendPut, removeEntity, useUserInfo, SaveResult, useDefaultHeaders } from "../client";
import { useEffect, useState } from "react";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { AxiosResponse } from "axios";
import ValidationSummary from "../components/ValidationSummary";

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
        } finally {
            setSaving(false);
        }
    }

    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Profil poskytovateľa dát'}]} />
            <MainContent>
                <PageHeader>Profil poskytovateľa dát</PageHeader>
                {profile ? <>
                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <FormElementGroup label="Adresa webového sídla" errorMessage={errors['homePage']} element={id => <BaseInput id={id} value={profile.website ?? ''} onChange={e => setProfile({...profile, website: e.target.value})} />} />
                    <FormElementGroup label="E-mailová adresa" errorMessage={errors['email']} element={id => <BaseInput id={id} value={profile.email ?? ''} onChange={e => setProfile({...profile, email: e.target.value})} />} />
                    <FormElementGroup label="Telefonický kontakt" errorMessage={errors['phone']} element={id => <BaseInput id={id} value={profile.phone ?? ''} onChange={e => setProfile({...profile, phone: e.target.value})} />} />

                    <Button style={{marginRight: '20px'}} onClick={async () => save} disabled={saving}>
                            Uložiť katalóg
                        </Button>
                </> : null}
            </MainContent>
        </>;
}