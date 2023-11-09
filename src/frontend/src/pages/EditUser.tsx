import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { useDocumentTitle, useUserEdit, useUserInfo } from "../client";
import MultiRadio from "../components/MultiRadio";
import Loading from "../components/Loading";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";

type Role = {
    id: string|null;
    name: string;
}

export default function EditUser()
{
    const [userInfo] = useUserInfo();
    const [inputs, , loading, setUser, errors, saving, save] = useUserEdit();
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('editUser'));

    const roles: Role[] = [
        {
            id: 'Publisher',
            name: t('publisherUser')
        },
        {
            id: 'PublisherAdmin',
            name: t('publisherAdmin')
        },
        {
            id: null,
            name: t('noRole')
        }
    ];

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('userList'), link: '/sprava/pouzivatelia'}, {title: t('editUser')}]} />
            <MainContent>
            <PageHeader>{t('editUser')}</PageHeader>
            {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {loading ? <Loading /> : null}

                    {!loading && inputs ? <>
                        <FormElementGroup label={t('firstName')} errorMessage={errors['firstname']} element={id => <BaseInput id={id} disabled={saving} value={inputs.firstName} onChange={e => setUser({firstName: e.target.value})} />} />
                        <FormElementGroup label={t('lastName')} errorMessage={errors['lastName']} element={id => <BaseInput id={id} disabled={saving} value={inputs.lastName} onChange={e => setUser({lastName: e.target.value})} />} />
                        <FormElementGroup label={t('emailAddress')} errorMessage={errors['email']} element={id => <BaseInput id={id} disabled={saving} value={inputs.email ?? ''} onChange={e => setUser({email: e.target.value})} />} />

                        <MultiRadio<Role> label={t('role')} 
                                        inline 
                                        disabled={saving}
                                        options={roles} 
                                        id="role-selection" 
                                        getValue={v => v.name} 
                                        renderOption={v => v.name} 
                                        selectedOption={roles.find(o => o.id === inputs.role) ?? roles[0]} 
                                        onChange={o => setUser({role: o.id})}  />
                    </> : null}
                    
                    <Button style={{marginRight: '20px'}} disabled={saving} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/pouzivatelia');
                        }
                    }}>
                        {t('save')} 
                    </Button>
            </MainContent>
        </>;
}