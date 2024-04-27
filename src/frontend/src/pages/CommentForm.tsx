import { yupResolver } from '@hookform/resolvers/yup';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { buildYup } from 'schema-to-yup';
import { useUserInfo } from '../client';
import { CommentFormValues, sendPost } from '../cms';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import TextArea from '../components/TextArea';
import { schema, schemaConfig } from './schemas/CommentSchema';
import { ROOT_ID } from '../helpers/helpers';

type Props = {
    contentId: string;
    parentId?: string;
    refresh: () => void;
};

export default function CommentForm(props: Props) {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();
    const { contentId, refresh, parentId } = props;
    const yupSchema = buildYup(schema, schemaConfig);

    const form = useForm<CommentFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            body: ''
        }
    });

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<CommentFormValues> = async (data) => {
        const request = {
            contentId,
            parentId: parentId ?? ROOT_ID,
            userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b1aa87',
            email: userInfo?.email || 'test@email.sk',
            body: data.body
        };
        const result = await sendPost<any>(`cms/comments`, request);
        if (result.status === 200) {
            refresh();
            reset();
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                <FormElementGroup label={t('comment.comment')} element={(id) => <TextArea id={id} rows={3} {...register('body')} />} />
                <Button type={'submit'} buttonType="primary" title={t('comment.add')}>
                    {t('comment.add')}
                </Button>
            </form>
        </>
    );
}

CommentForm.defaultProps = {
    readonly: false,
    parentId: null
};
