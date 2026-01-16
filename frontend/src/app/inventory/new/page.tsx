'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
} from '@/components/ui';
import { useCreateProduct } from '@/hooks';
import { ArrowLeft, Plus, Trash2, Package } from 'lucide-react';

const variantSchema = z.object({
  sku: z.string().min(1, 'SKU is required'),
  name: z.string().min(1, 'Name is required'),
  option1Name: z.string().optional(),
  option1Value: z.string().optional(),
  option2Name: z.string().optional(),
  option2Value: z.string().optional(),
  costPrice: z.coerce.number().min(0).optional(),
  sellingPrice: z.coerce.number().min(0).optional(),
  weight: z.coerce.number().min(0).optional(),
  initialStock: z.coerce.number().int().min(0).default(0),
});

const productSchema = z.object({
  sku: z.string().min(1, 'SKU is required'),
  name: z.string().min(1, 'Product name is required'),
  description: z.string().optional(),
  category: z.string().optional(),
  brand: z.string().optional(),
  costPrice: z.coerce.number().min(0, 'Cost price must be 0 or greater'),
  sellingPrice: z.coerce.number().min(0, 'Selling price must be 0 or greater'),
  currency: z.string().default('INR'),
  weight: z.coerce.number().min(0).optional(),
  imageUrl: z.string().url().optional().or(z.literal('')),
  hsnCode: z.string().optional(),
  taxRate: z.coerce.number().min(0).max(100).optional(),
  initialStock: z.coerce.number().int().min(0).default(0),
  variants: z.array(variantSchema).optional(),
});

type ProductFormData = z.infer<typeof productSchema>;

const categoryOptions = [
  { value: '', label: 'Select Category' },
  { value: 'Electronics', label: 'Electronics' },
  { value: 'Clothing', label: 'Clothing' },
  { value: 'Home & Garden', label: 'Home & Garden' },
  { value: 'Sports', label: 'Sports' },
  { value: 'Books', label: 'Books' },
  { value: 'Health & Beauty', label: 'Health & Beauty' },
  { value: 'Toys', label: 'Toys' },
  { value: 'Food & Beverages', label: 'Food & Beverages' },
  { value: 'Other', label: 'Other' },
];

export default function NewProductPage() {
  const router = useRouter();
  const createProductMutation = useCreateProduct();
  const [showVariants, setShowVariants] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      currency: 'INR',
      costPrice: 0,
      sellingPrice: 0,
      initialStock: 0,
      variants: [],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'variants',
  });

  const onSubmit = async (data: ProductFormData) => {
    try {
      const result = await createProductMutation.mutateAsync({
        ...data,
        imageUrl: data.imageUrl || undefined,
        variants: showVariants && data.variants?.length ? data.variants : undefined,
      });
      router.push(`/inventory/${result.id}`);
    } catch (err) {
      console.error('Failed to create product:', err);
    }
  };

  const addVariant = () => {
    append({
      sku: '',
      name: '',
      option1Name: 'Size',
      option1Value: '',
      initialStock: 0,
    });
  };

  return (
    <DashboardLayout title="Add Product">
      <div className="mb-6 flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => router.back()}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold">Add New Product</h1>
          <p className="text-sm text-muted-foreground">Create a new product in your inventory</p>
        </div>
      </div>

      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            {/* Basic Info */}
            <Card>
              <CardHeader>
                <CardTitle>Basic Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      SKU <span className="text-error">*</span>
                    </label>
                    <Input {...register('sku')} placeholder="e.g., PROD-001" />
                    {errors.sku && (
                      <p className="text-sm text-error mt-1">{errors.sku.message}</p>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      Product Name <span className="text-error">*</span>
                    </label>
                    <Input {...register('name')} placeholder="Enter product name" />
                    {errors.name && (
                      <p className="text-sm text-error mt-1">{errors.name.message}</p>
                    )}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">Description</label>
                  <textarea
                    {...register('description')}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm min-h-[100px]"
                    placeholder="Enter product description"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-1">Category</label>
                    <Select options={categoryOptions} {...register('category')} />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">Brand</label>
                    <Input {...register('brand')} placeholder="Enter brand name" />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">Image URL</label>
                  <Input {...register('imageUrl')} placeholder="https://..." type="url" />
                </div>
              </CardContent>
            </Card>

            {/* Pricing */}
            <Card>
              <CardHeader>
                <CardTitle>Pricing</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      Cost Price <span className="text-error">*</span>
                    </label>
                    <Input
                      {...register('costPrice')}
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                    />
                    {errors.costPrice && (
                      <p className="text-sm text-error mt-1">{errors.costPrice.message}</p>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      Selling Price <span className="text-error">*</span>
                    </label>
                    <Input
                      {...register('sellingPrice')}
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                    />
                    {errors.sellingPrice && (
                      <p className="text-sm text-error mt-1">{errors.sellingPrice.message}</p>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">Currency</label>
                    <Select
                      options={[
                        { value: 'INR', label: 'INR' },
                        { value: 'USD', label: 'USD' },
                      ]}
                      {...register('currency')}
                    />
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Variants */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Variants</CardTitle>
                <Button
                  type="button"
                  variant={showVariants ? 'outline' : 'default'}
                  size="sm"
                  onClick={() => setShowVariants(!showVariants)}
                >
                  {showVariants ? 'Disable Variants' : 'Enable Variants'}
                </Button>
              </CardHeader>
              {showVariants && (
                <CardContent className="space-y-4">
                  {fields.length === 0 ? (
                    <div className="text-center py-8 text-muted-foreground">
                      <Package className="h-12 w-12 mx-auto mb-2 opacity-50" />
                      <p>No variants added yet</p>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="mt-2"
                        onClick={addVariant}
                      >
                        Add First Variant
                      </Button>
                    </div>
                  ) : (
                    <>
                      {fields.map((field, index) => (
                        <div key={field.id} className="rounded-lg border p-4 space-y-3">
                          <div className="flex items-center justify-between">
                            <span className="font-medium">Variant {index + 1}</span>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => remove(index)}
                            >
                              <Trash2 className="h-4 w-4 text-error" />
                            </Button>
                          </div>
                          <div className="grid grid-cols-2 gap-3">
                            <div>
                              <label className="block text-xs font-medium mb-1">SKU</label>
                              <Input
                                {...register(`variants.${index}.sku`)}
                                placeholder="Variant SKU"
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium mb-1">Name</label>
                              <Input
                                {...register(`variants.${index}.name`)}
                                placeholder="Variant name"
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium mb-1">Option Name</label>
                              <Input
                                {...register(`variants.${index}.option1Name`)}
                                placeholder="e.g., Size"
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium mb-1">Option Value</label>
                              <Input
                                {...register(`variants.${index}.option1Value`)}
                                placeholder="e.g., Large"
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium mb-1">
                                Selling Price (Override)
                              </label>
                              <Input
                                {...register(`variants.${index}.sellingPrice`)}
                                type="number"
                                step="0.01"
                                min="0"
                                placeholder="Optional"
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium mb-1">Initial Stock</label>
                              <Input
                                {...register(`variants.${index}.initialStock`)}
                                type="number"
                                min="0"
                                placeholder="0"
                              />
                            </div>
                          </div>
                        </div>
                      ))}
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={addVariant}
                        leftIcon={<Plus className="h-4 w-4" />}
                      >
                        Add Another Variant
                      </Button>
                    </>
                  )}
                </CardContent>
              )}
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Inventory */}
            <Card>
              <CardHeader>
                <CardTitle>Inventory</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Initial Stock</label>
                  <Input
                    {...register('initialStock')}
                    type="number"
                    min="0"
                    placeholder="0"
                  />
                  <p className="text-xs text-muted-foreground mt-1">
                    {showVariants
                      ? 'Set stock for each variant above'
                      : 'Initial stock quantity for this product'}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Weight (grams)</label>
                  <Input
                    {...register('weight')}
                    type="number"
                    step="0.1"
                    min="0"
                    placeholder="Optional"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Tax Info */}
            <Card>
              <CardHeader>
                <CardTitle>Tax Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-1">HSN Code</label>
                  <Input {...register('hsnCode')} placeholder="e.g., 6109" />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Tax Rate (%)</label>
                  <Input
                    {...register('taxRate')}
                    type="number"
                    step="0.1"
                    min="0"
                    max="100"
                    placeholder="e.g., 18"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Actions */}
            <Card>
              <CardContent className="p-4 space-y-2">
                <Button
                  type="submit"
                  className="w-full"
                  isLoading={isSubmitting || createProductMutation.isPending}
                >
                  Create Product
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="w-full"
                  onClick={() => router.back()}
                >
                  Cancel
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </form>
    </DashboardLayout>
  );
}
